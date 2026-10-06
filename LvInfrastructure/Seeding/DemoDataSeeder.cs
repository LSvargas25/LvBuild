using LvApplication.Common;
using LvApplication.DTOs.Branches;
using LvApplication.DTOs.Budgets;
using LvApplication.DTOs.Commercial;
using LvApplication.DTOs.Customers;
using LvApplication.DTOs.Incidents;
using LvApplication.DTOs.Inventory;
using LvApplication.DTOs.Offers;
using LvApplication.DTOs.Payroll;
using LvApplication.DTOs.Projects;
using LvApplication.DTOs.SiteLogs;
using LvApplication.DTOs.Suppliers;
using LvApplication.DTOs.Warehouse;
using LvApplication.DTOs.Workers;
using LvApplication.Services.Branches;
using LvApplication.Services.Budgets;
using LvApplication.Services.Commercial;
using LvApplication.Services.Customers;
using LvApplication.Services.Incidents;
using LvApplication.Services.Inventory;
using LvApplication.Services.Offers;
using LvApplication.Services.Payroll;
using LvApplication.Services.Projects;
using LvApplication.Services.SiteLogs;
using LvApplication.Services.Suppliers;
using LvApplication.Services.Warehouse;
using LvApplication.Services.Workers;
using LvDomain.Entities.Auth;
using LvDomain.Entities.Materials;
using LvDomain.Enums;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace LvInfrastructure.Seeding;

/// <summary>
/// Loads a believable demo company (Seed:Demo=true). Every business flow goes through the real
/// application services, so totals, stock, project expenses and chapter costs are computed by the
/// same rules as in production. Dates are relative to "today" in Costa Rica so the demo always
/// looks current: the project starts on the Monday five weeks before the seed, and a final pass
/// (DemoDataSeeder.Timeline.cs) moves the timestamps the services stamp with "now" to the week
/// each event belongs to.
///
/// Idempotent: if the demo General Manager already exists nothing is done. On PostgreSQL
/// everything runs in one transaction, so a failed run leaves no partial data behind.
/// </summary>
public sealed partial class DemoDataSeeder
{
    public const string DemoPassword = "LvBuild#2026";

    public static readonly IReadOnlyList<DemoAccount> Accounts =
    [
        new("gerencia@lvbuild.test", "Ana Lucía Vargas", "GeneralManager"),
        new("operaciones@lvbuild.test", "Roberto Quesada", "OperationsDirector"),
        new("proyectos@lvbuild.test", "María Fernanda Solano", "ProjectAdmin"),
        new("sucursal@lvbuild.test", "Diego Araya", "BranchAdmin"),
        new("comercial@lvbuild.test", "Gabriela Méndez", "BusinessManager"),
    ];

    private static readonly string[] GeneralManagerRoles = ["GeneralManager"];

    private readonly AppDbContext _context;
    private readonly IBranchService _branches;
    private readonly ICustomerService _customers;
    private readonly ISupplierService _suppliers;
    private readonly IWorkerService _workers;
    private readonly IProductService _products;
    private readonly IProductIncorporationTicketService _incorporationTickets;
    private readonly IInventoryMovementService _movements;
    private readonly ICashRegisterService _cashRegisters;
    private readonly IInvoiceService _invoices;
    private readonly IBudgetService _budgets;
    private readonly IOfferService _offers;
    private readonly IProjectService _projects;
    private readonly IMaterialTicketService _materialTickets;
    private readonly ISiteLogService _siteLogs;
    private readonly IPayrollService _payrolls;
    private readonly IIncidentService _incidents;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(
        AppDbContext context,
        IBranchService branches,
        ICustomerService customers,
        ISupplierService suppliers,
        IWorkerService workers,
        IProductService products,
        IProductIncorporationTicketService incorporationTickets,
        IInventoryMovementService movements,
        ICashRegisterService cashRegisters,
        IInvoiceService invoices,
        IBudgetService budgets,
        IOfferService offers,
        IProjectService projects,
        IMaterialTicketService materialTickets,
        ISiteLogService siteLogs,
        IPayrollService payrolls,
        IIncidentService incidents,
        ILogger<DemoDataSeeder> logger
    )
    {
        _context = context;
        _branches = branches;
        _customers = customers;
        _suppliers = suppliers;
        _workers = workers;
        _products = products;
        _incorporationTickets = incorporationTickets;
        _movements = movements;
        _cashRegisters = cashRegisters;
        _invoices = invoices;
        _budgets = budgets;
        _offers = offers;
        _projects = projects;
        _materialTickets = materialTickets;
        _siteLogs = siteLogs;
        _payrolls = payrolls;
        _incidents = incidents;
        _logger = logger;
    }

    /// <returns>True if the demo data was created, false if it already existed.</returns>
    public async Task<bool> SeedAsync(CancellationToken cancellationToken = default)
    {
        var markerEmail = Accounts[0].Email;
        if (await _context.Users.AnyAsync(u => u.Email == markerEmail, cancellationToken))
        {
            LogAlreadySeeded(_logger);
            return false;
        }

        // InMemory (unit tests) has no transactions; PostgreSQL gets all-or-nothing.
        await using IDbContextTransaction? transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var users = await SeedUsersAsync(cancellationToken);
        var company = await SeedCompanyAsync(users);
        await SeedStoreAsync(users, company);
        await SeedBudgetsInEveryStateAsync(users, company);
        var timeline = DemoTimeline.From(CostaRicaTime.Today);
        await SeedActiveProjectAsync(users, company, timeline);
        await ApplyTimelineAsync(timeline, cancellationToken);

        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);

        LogSeeded(_logger, Accounts.Count);
        return true;
    }

    // ---------------------------------------------------------------- users

    private async Task<DemoUsers> SeedUsersAsync(CancellationToken cancellationToken)
    {
        var roleIds = await _context.Roles.ToDictionaryAsync(
            r => r.Name,
            r => r.Id,
            cancellationToken
        );
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword);
        var created = new Dictionary<string, User>();

        foreach (var account in Accounts)
        {
            var user = new User
            {
                Name = account.Name,
                Email = EmailNormalizer.Normalize(account.Email),
                PasswordHash = passwordHash,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            user.UserRoles.Add(new UserRole { RoleId = roleIds[account.Role] });
            _context.Users.Add(user);
            created[account.Role] = user;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new DemoUsers(
            created["GeneralManager"].Id,
            created["OperationsDirector"].Id,
            created["ProjectAdmin"].Id,
            created["BranchAdmin"].Id,
            created["BusinessManager"].Id
        );
    }

    // ---------------------------------------------------------------- master data

    private async Task<DemoCompany> SeedCompanyAsync(DemoUsers users)
    {
        var office = await _branches.CreateAsync(
            new CreateBranchDto
            {
                Name = "Oficina Central San José",
                City = "San José",
                Province = "San José",
                PhoneNumber = "2222-1000",
                Email = "oficina@lvbuild.test",
                BranchType = BranchType.Office,
                OperationsDirectorId = users.OperationsDirector,
            }
        );
        var store = await _branches.CreateAsync(
            new CreateBranchDto
            {
                Name = "Ferretería LV Heredia",
                City = "Heredia",
                Province = "Heredia",
                PhoneNumber = "2260-4500",
                Email = "ferreteria@lvbuild.test",
                BranchType = BranchType.Commercial,
                OperationsDirectorId = users.OperationsDirector,
                BranchAdminId = users.BranchAdmin,
                BusinessManagerId = users.BusinessManager,
            }
        );
        var warehouse = await _branches.CreateAsync(
            new CreateBranchDto
            {
                Name = "Bodega Central Cartago",
                City = "Cartago",
                Province = "Cartago",
                PhoneNumber = "2551-7800",
                BranchType = BranchType.Warehouse,
                OperationsDirectorId = users.OperationsDirector,
                BranchAdminId = users.BranchAdmin,
            }
        );

        var customers = new List<int>();
        foreach (
            var (name, city, phone, personalId) in new[]
            {
                ("Familia Mora Rodríguez", "Escazú", "8845-2210", "1-1234-0567"),
                ("Inversiones Ochomogo S.A.", "Cartago", "2573-9900", "3-101-456789"),
                ("Grupo Ícaro Consultores", "San Pedro", "2283-6100", "3-101-778812"),
                ("Asociación Colegio San Rafael", "Alajuela", "2438-1200", "3-002-114455"),
                ("Lucía Fallas Vega", "Tamarindo", "8710-3344", "5-0456-0789"),
                ("Desarrollos Curridabat Ltda.", "Curridabat", "2271-5050", "3-102-660011"),
            }
        )
        {
            var customer = await _customers.CreateAsync(
                new CreateCustomerDto
                {
                    Name = name,
                    CustomerType = CustomerType.Project,
                    City = city,
                    PhoneNumber = phone,
                    PersonalId = personalId,
                    Email = $"cliente{customers.Count + 1}@lvbuild.test",
                }
            );
            customers.Add(customer.Id);
        }

        var storeCustomer = await _customers.CreateAsync(
            new CreateCustomerDto
            {
                Name = "Constructora Hermanos Rojas",
                CustomerType = CustomerType.Store,
                City = "Heredia",
                PhoneNumber = "2237-4411",
                PersonalId = "3-101-332211",
                BranchId = store.Id,
            }
        );

        var suppliers = new List<int>();
        foreach (
            var (name, city) in new[]
            {
                ("Cementos del Valle S.A.", "Cartago"),
                ("Aceros y Varillas de Costa Rica", "Alajuela"),
                ("Agregados La Uruca", "San José"),
            }
        )
        {
            var supplier = await _suppliers.CreateAsync(
                new CreateSupplierDto
                {
                    Name = name,
                    City = city,
                    Email = $"ventas{suppliers.Count + 1}@proveedor.lvbuild.test",
                }
            );
            suppliers.Add(supplier.Id);
        }

        var crew = new List<(int Id, decimal HourlyRate)>();
        foreach (
            var (name, type, rate) in new[]
            {
                ("Carlos Jiménez Brenes", WorkerType.SiteForeman, 4_500m),
                ("José Vargas Calderón", WorkerType.Laborer, 2_800m),
                ("Luis Mora Picado", WorkerType.Laborer, 2_800m),
                ("Andrés Solís Ureña", WorkerType.ConstructionHelper, 2_300m),
                ("Mario Rojas Cordero", WorkerType.HeavyEquipmentOperator, 4_000m),
            }
        )
        {
            var worker = await _workers.CreateAsync(
                new CreateWorkerDto
                {
                    Name = name,
                    Category = WorkerCategory.Construction,
                    Type = type,
                    HourlyRate = rate,
                    BranchId = office.Id,
                }
            );
            crew.Add((worker.Id, rate));
        }

        await _workers.CreateAsync(
            new CreateWorkerDto
            {
                Name = "Ing. Daniela Castro Mena",
                Category = WorkerCategory.Office,
                Type = WorkerType.Engineer,
                HourlyRate = 9_000m,
                BranchId = office.Id,
            }
        );
        await _workers.CreateAsync(
            new CreateWorkerDto
            {
                Name = "Esteban Navarro Chaves",
                Category = WorkerCategory.Storage,
                Type = WorkerType.WarehouseKeeper,
                HourlyRate = 2_600m,
                BranchId = warehouse.Id,
            }
        );

        var materials = new Dictionary<string, int>();
        foreach (
            var (key, name, unit) in new[]
            {
                ("cement", "Cemento gris 50 kg", "saco"),
                ("rebar", "Varilla corrugada #3", "unidad"),
                ("block", "Block de concreto 15x20x40", "unidad"),
                ("sand", "Arena de tajo", "m3"),
            }
        )
        {
            var material = new MaterialCatalog
            {
                Name = name,
                UnitOfMeasure = unit,
                CreatedAt = DateTime.UtcNow,
            };
            _context.MaterialCatalogs.Add(material);
            await _context.SaveChangesAsync();
            materials[key] = material.Id;
        }

        return new DemoCompany(
            office.Id,
            store.Id,
            warehouse.Id,
            customers,
            storeCustomer.Id,
            suppliers,
            crew,
            materials
        );
    }

    // ---------------------------------------------------------------- store (POS + warehouse)

    private async Task SeedStoreAsync(DemoUsers users, DemoCompany company)
    {
        var products = new List<int>();
        foreach (
            var (name, sku, unit, price, cost) in new[]
            {
                ("Cemento gris 50 kg", "CEM-050", "saco", 7_200m, 5_600m),
                ("Varilla corrugada #3 (6 m)", "VAR-003", "unidad", 2_950m, 2_100m),
                ("Block de concreto 15x20x40", "BLK-15", "unidad", 650m, 420m),
                ("Pintura acrílica blanca (galón)", "PIN-GAL", "galón", 18_500m, 13_000m),
                ("Tornillo gypsum 1\" (caja 100)", "TOR-100", "caja", 3_200m, 2_000m),
            }
        )
        {
            var product = await _products.CreateAsync(
                new CreateProductDto
                {
                    Name = name,
                    Sku = sku,
                    UnitOfMeasure = unit,
                    UnitPrice = price,
                    UnitCost = cost,
                    Category = "Materiales de construcción",
                },
                users.GeneralManager,
                GeneralManagerRoles
            );
            products.Add(product.Id);

            // Stock arrives at the warehouse; the General Manager's tickets auto-validate.
            await _incorporationTickets.CreateAsync(
                new CreateProductIncorporationTicketDto
                {
                    BranchId = company.Warehouse,
                    ProductId = product.Id,
                    SupplierId = company.Suppliers[0],
                    Quantity = 400,
                    UnitCost = cost,
                },
                users.GeneralManager,
                GeneralManagerRoles
            );
        }

        // Warehouse -> store transfers, validated on arrival.
        foreach (
            var (productIndex, quantity) in new[]
            {
                (0, 120m),
                (1, 150m),
                (2, 300m),
                (3, 25m),
                (4, 40m),
            }
        )
        {
            var movement = await _movements.CreateAsync(
                new CreateInventoryMovementDto
                {
                    OriginBranchId = company.Warehouse,
                    DestinationBranchId = company.Store,
                    ProductId = products[productIndex],
                    Quantity = quantity,
                },
                users.BranchAdmin
            );
            await _movements.ValidateAsync(
                movement.Id,
                approve: true,
                users.OperationsDirector,
                ["OperationsDirector"]
            );
        }

        var register = await _cashRegisters.OpenAsync(
            new OpenCashRegisterDto { BranchId = company.Store, OpeningBalance = 50_000m },
            users.BranchAdmin
        );

        // Walk-in cash sale, paid in full.
        var cashSale = await _invoices.CreateAsync(
            new CreateInvoiceDto
            {
                BranchId = company.Store,
                CashRegisterId = register.Id,
                PaymentType = InvoicePaymentType.Contado,
                Details =
                [
                    new InvoiceDetailLineDto { ProductId = products[0], Quantity = 10 },
                    new InvoiceDetailLineDto { ProductId = products[2], Quantity = 80 },
                ],
            },
            users.BranchAdmin
        );
        await _invoices.IssueAsync(
            cashSale.Id,
            new IssueInvoiceDto
            {
                Payments =
                [
                    new CreateInvoicePaymentDto
                    {
                        PaymentMethod = InvoicePaymentMethod.Sinpe,
                        Amount = cashSale.Total,
                    },
                ],
            },
            users.BranchAdmin
        );

        // Credit sale to a contractor: 50% at issue, the rest later -> fully paid.
        var creditPaid = await _invoices.CreateAsync(
            new CreateInvoiceDto
            {
                BranchId = company.Store,
                CashRegisterId = register.Id,
                CustomerId = company.StoreCustomer,
                PaymentType = InvoicePaymentType.Credito,
                Details =
                [
                    new InvoiceDetailLineDto { ProductId = products[1], Quantity = 40 },
                    new InvoiceDetailLineDto { ProductId = products[4], Quantity = 6 },
                ],
            },
            users.BranchAdmin
        );
        var half = decimal.Round(creditPaid.Total / 2, 2);
        await _invoices.IssueAsync(
            creditPaid.Id,
            new IssueInvoiceDto
            {
                Payments =
                [
                    new CreateInvoicePaymentDto
                    {
                        PaymentMethod = InvoicePaymentMethod.Tarjeta,
                        Amount = half,
                    },
                ],
            },
            users.BranchAdmin
        );
        await _invoices.AddPaymentAsync(
            creditPaid.Id,
            new CreateInvoicePaymentDto
            {
                PaymentMethod = InvoicePaymentMethod.Efectivo,
                Amount = creditPaid.Total - half,
            },
            users.BranchAdmin
        );

        // Credit sale with an outstanding balance.
        var creditOpen = await _invoices.CreateAsync(
            new CreateInvoiceDto
            {
                BranchId = company.Store,
                CashRegisterId = register.Id,
                CustomerId = company.StoreCustomer,
                PaymentType = InvoicePaymentType.Credito,
                Details =
                [
                    new InvoiceDetailLineDto { ProductId = products[3], Quantity = 4 },
                    new InvoiceDetailLineDto { ProductId = products[0], Quantity = 15 },
                ],
            },
            users.BranchAdmin
        );
        await _invoices.IssueAsync(
            creditOpen.Id,
            new IssueInvoiceDto
            {
                Payments =
                [
                    new CreateInvoicePaymentDto
                    {
                        PaymentMethod = InvoicePaymentMethod.Efectivo,
                        Amount = 30_000m,
                    },
                ],
            },
            users.BranchAdmin
        );
    }

    // ---------------------------------------------------------------- budgets

    private async Task SeedBudgetsInEveryStateAsync(DemoUsers users, DemoCompany company)
    {
        // Draft
        await CreateBudgetAsync(
            users,
            company,
            company.Customers[1],
            "Bodega comercial Ochomogo",
            SmallChapters("Nave industrial", 9_800_000m)
        );

        // Review
        var review = await CreateBudgetAsync(
            users,
            company,
            company.Customers[2],
            "Remodelación oficinas Grupo Ícaro",
            SmallChapters("Remodelación interior", 4_300_000m)
        );
        await _budgets.SubmitForReviewAsync(review, users.ProjectAdmin);

        // Correction requested by management
        var correction = await CreateBudgetAsync(
            users,
            company,
            company.Customers[3],
            "Ampliación Colegio San Rafael (3 aulas)",
            SmallChapters("Aulas nuevas", 18_500_000m)
        );
        await _budgets.SubmitForReviewAsync(correction, users.ProjectAdmin);
        await _budgets.RequestCorrectionAsync(
            correction,
            new RequestCorrectionDto
            {
                Comment =
                    "Separar la instalación eléctrica en su propio capítulo y revisar el costo del techo.",
            },
            users.GeneralManager
        );

        // Approved internally and sent, waiting for the client
        var sent = await CreateBudgetAsync(
            users,
            company,
            company.Customers[4],
            "Casa de playa Tamarindo",
            SmallChapters("Vivienda unifamiliar", 52_000_000m)
        );
        await _budgets.SubmitForReviewAsync(sent, users.ProjectAdmin);
        await _budgets.ApproveInternalAsync(sent, users.GeneralManager);

        // Cancelled
        var cancelled = await CreateBudgetAsync(
            users,
            company,
            company.Customers[5],
            "Local comercial Curridabat",
            SmallChapters("Local comercial", 12_700_000m)
        );
        await _budgets.CancelAsync(
            cancelled,
            new CancelBudgetDto
            {
                Reason = "El cliente pospuso la inversión hasta el próximo año.",
            },
            users.GeneralManager
        );
    }

    private async Task<int> CreateBudgetAsync(
        DemoUsers users,
        DemoCompany company,
        int customerId,
        string name,
        List<BudgetChapterDto> chapters
    )
    {
        var budget = await _budgets.CreateAsync(
            new CreateBudgetDto
            {
                CustomerId = customerId,
                BranchId = company.Office,
                Name = name,
                UtilityPercentage = 15,
                IndirectCostsTotal = 650_000m,
                Chapters = chapters,
            },
            users.ProjectAdmin
        );
        return budget.Id;
    }

    /// <summary>One chapter split roughly 45/40/15 into materials, labor and equipment.</summary>
    private static List<BudgetChapterDto> SmallChapters(string name, decimal directCost) =>
        [
            new BudgetChapterDto
            {
                Name = name,
                Order = 1,
                EstimatedWeeks = 8,
                Activities =
                [
                    Activity(
                        "Obra gris",
                        decimal.Round(directCost * 0.45m),
                        decimal.Round(directCost * 0.40m),
                        decimal.Round(directCost * 0.15m)
                    ),
                ],
            },
        ];

    private static BudgetActivityDto Activity(
        string description,
        decimal materials,
        decimal labor,
        decimal equipment
    ) =>
        new()
        {
            Description = description,
            MaterialCost = materials,
            LaborCost = labor,
            EquipmentCost = equipment,
        };

    // ---------------------------------------------------------------- active project

    /// <summary>
    /// Residencia Familia Mora: budget -> offer (accepted) -> project started five weeks ago,
    /// with material purchases, four approved weekly site logs (three paid payrolls and the
    /// latest one pending), one site log in review, the current week in draft and an approved
    /// incident.
    /// </summary>
    private async Task SeedActiveProjectAsync(
        DemoUsers users,
        DemoCompany company,
        DemoTimeline timeline
    )
    {
        var budget = await _budgets.CreateAsync(
            new CreateBudgetDto
            {
                CustomerId = company.Customers[0],
                BranchId = company.Office,
                Name = "Residencia Familia Mora – Escazú",
                UtilityPercentage = 15,
                IndirectCostsTotal = 1_200_000m,
                Chapters =
                [
                    new BudgetChapterDto
                    {
                        Name = "Obras preliminares y cimentación",
                        Order = 1,
                        EstimatedWeeks = 3,
                        Activities =
                        [
                            Activity("Trazo, limpieza y nivelación", 150_000m, 280_000m, 90_000m),
                            Activity(
                                "Excavación, zapatas y vigas de fundación",
                                1_450_000m,
                                980_000m,
                                350_000m
                            ),
                        ],
                    },
                    new BudgetChapterDto
                    {
                        Name = "Estructura y paredes",
                        Order = 2,
                        EstimatedWeeks = 6,
                        Activities =
                        [
                            Activity(
                                "Columnas y vigas de concreto",
                                2_600_000m,
                                1_750_000m,
                                420_000m
                            ),
                            Activity("Paredes de block", 1_900_000m, 1_300_000m, 120_000m),
                        ],
                    },
                    new BudgetChapterDto
                    {
                        Name = "Techos y acabados",
                        Order = 3,
                        EstimatedWeeks = 7,
                        Activities =
                        [
                            Activity(
                                "Estructura de techo y cubierta",
                                2_100_000m,
                                950_000m,
                                180_000m
                            ),
                            Activity("Repellos, pintura y pisos", 1_750_000m, 1_400_000m, 90_000m),
                        ],
                    },
                ],
            },
            users.ProjectAdmin
        );
        await _budgets.SubmitForReviewAsync(budget.Id, users.ProjectAdmin);
        await _budgets.ApproveInternalAsync(budget.Id, users.GeneralManager);

        var chapters = budget.Chapters.OrderBy(c => c.Order).Select(c => c.Id).ToList();
        var thisMonday = timeline.ThisMonday;
        var startDate = timeline.ProjectStart;

        var offer = await _offers.CreateAsync(
            new CreateOfferDto
            {
                BudgetId = budget.Id,
                OfferType = OfferType.Turnkey,
                IssueDate = startDate.AddDays(-21),
                ValidityDays = 30,
                WorkLocation = "Escazú, San Rafael, 300 m sur de la iglesia",
                WorkScope =
                    "Construcción llave en mano de vivienda de dos plantas (210 m²): cimentación, estructura, paredes, techo y acabados.",
                EstimatedStartDate = startDate,
                EstimatedDurationWeeks = 16,
                PaymentTerms = "30% al firmar, 40% contra avance de obra gris, 30% a la entrega.",
                Warranties = "Garantía estructural de 5 años y de acabados de 1 año.",
                Exclusions = "No incluye mobiliario, electrodomésticos ni permisos municipales.",
                // Rounded to the thousand colones, as a commercial price.
                TotalProjectPrice = Math.Round(budget.TotalBudget / 1000m) * 1000m,
            },
            users.ProjectAdmin
        );
        await _offers.SendToClientAsync(offer.Id);
        await _offers.MarkAcceptedAsync(offer.Id, users.GeneralManager);

        var project = await _projects.CreateProjectAsync(
            new CreateProjectDto
            {
                OfferId = offer.Id,
                BranchId = company.Office,
                StartDate = startDate,
            },
            users.GeneralManager
        );
        foreach (var (workerId, _) in company.Crew)
        {
            await _projects.AssignWorkerAsync(
                project.Id,
                new AssignWorkerDto { WorkerId = workerId },
                users.GeneralManager
            );
        }

        // Material purchases, applied to the project inventory.
        foreach (
            var (material, quantity, unitPrice, chapter) in new[]
            {
                ("cement", 140m, 5_600m, 0),
                ("sand", 20m, 14_500m, 0),
                ("rebar", 320m, 2_050m, 1),
                ("block", 1_900m, 410m, 1),
            }
        )
        {
            var ticket = await _materialTickets.CreateAsync(
                project.Id,
                new CreateMaterialTicketDto
                {
                    SupplierId = material is "rebar" ? company.Suppliers[1] : company.Suppliers[0],
                    MaterialId = company.Materials[material],
                    Description = $"Compra de {quantity:0} unidades para la obra",
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    ChapterId = chapters[chapter],
                },
                users.ProjectAdmin
            );
            await _materialTickets.ApplyAsync(ticket.Id);
        }

        // Weekly site logs. Week 0..3 approved, week 4 in review, current week draft.
        var weeks = new[]
        {
            (
                Chapter: 0,
                Task: "Trazo, limpieza del terreno y excavación de zapatas.",
                Materials: new[] { ("cement", 30m), ("sand", 6m) }
            ),
            (
                Chapter: 0,
                Task: "Armado y colado de zapatas y vigas de fundación.",
                Materials: new[] { ("cement", 45m), ("sand", 8m) }
            ),
            (
                Chapter: 1,
                Task: "Columnas de primera planta y primeras hiladas de block.",
                Materials: new[] { ("rebar", 140m), ("block", 650m), ("cement", 20m) }
            ),
            (
                Chapter: 1,
                Task: "Paredes de primera planta y viga corona.",
                Materials: new[] { ("rebar", 110m), ("block", 720m), ("cement", 15m) }
            ),
            (
                Chapter: 1,
                Task: "Entrepiso: formaleta, armado y colado.",
                Materials: new[] { ("rebar", 40m), ("cement", 10m) }
            ),
        };

        var siteLogIds = new List<int>();
        for (var week = 0; week < weeks.Length; week++)
        {
            var weekStart = startDate.AddDays(7 * week);
            var siteLog = await _siteLogs.CreateAsync(
                new CreateSiteLogDto
                {
                    ProjectId = project.Id,
                    ChapterId = chapters[weeks[week].Chapter],
                    WeekStart = weekStart,
                    WeekEnd = weekStart.AddDays(6),
                    TaskDescription = weeks[week].Task,
                    PendingTasks =
                        week == weeks.Length - 1
                            ? "Curado del entrepiso antes de continuar."
                            : null,
                    Workers = company
                        .Crew.Select(w => new SiteLogWorkerDto
                        {
                            WorkerId = w.Id,
                            HoursWorked = 48,
                        })
                        .ToList(),
                    Materials = weeks[week]
                        .Materials.Select(m => new SiteLogMaterialDto
                        {
                            MaterialId = company.Materials[m.Item1],
                            QuantityUsed = m.Item2,
                        })
                        .ToList(),
                },
                users.ProjectAdmin
            );
            await _siteLogs.SubmitToReviewAsync(siteLog.Id);
            if (week < 4)
                await _siteLogs.ApproveAsync(siteLog.Id, users.OperationsDirector);
            siteLogIds.Add(siteLog.Id);
        }

        await _siteLogs.CreateAsync(
            new CreateSiteLogDto
            {
                ProjectId = project.Id,
                ChapterId = chapters[1],
                WeekStart = thisMonday,
                WeekEnd = thisMonday.AddDays(6),
                TaskDescription = "Paredes de segunda planta (en curso).",
                Workers = company
                    .Crew.Select(w => new SiteLogWorkerDto { WorkerId = w.Id, HoursWorked = 16 })
                    .ToList(),
            },
            users.ProjectAdmin
        );

        // Payrolls for the approved weeks: all paid except the latest one, still pending.
        for (var week = 0; week < 4; week++)
        {
            var weekEnd = startDate.AddDays((7 * week) + 6);
            var payroll = await _payrolls.CreateAsync(
                new CreatePayrollDto
                {
                    SiteLogId = siteLogIds[week],
                    ChapterId = chapters[weeks[week].Chapter],
                    Details = company
                        .Crew.Select(w => new PayrollDetailDto
                        {
                            WorkerId = w.Id,
                            Date = weekEnd,
                            HoursWorked = 48,
                            HourlyRate = w.HourlyRate,
                            PaymentType = PayrollPaymentType.Full,
                            Payments =
                            [
                                new PayrollDetailPaymentDto
                                {
                                    PaymentMethod = PaymentMethod.Transfer,
                                    Amount = 48 * w.HourlyRate,
                                },
                            ],
                        })
                        .ToList(),
                },
                users.ProjectAdmin
            );
            if (week < 3)
                await _payrolls.MarkAsPaidAsync(payroll.Id);
        }

        // An unplanned event, approved: it adds to the chapter's actual cost.
        var incident = await _incidents.CreateAsync(
            new CreateIncidentDto
            {
                ProjectId = project.Id,
                ChapterId = chapters[1],
                Date = startDate.AddDays(DemoTimeline.IncidentDay),
                Description =
                    "Lluvia intensa dañó la formaleta de dos columnas; se repuso y se volvió a colar.",
                Materials =
                [
                    new IncidentMaterialDto
                    {
                        MaterialId = company.Materials["cement"],
                        Quantity = 8,
                    },
                ],
                Workers =
                [
                    new IncidentWorkerDto { WorkerId = company.Crew[1].Id, HoursUsed = 8 },
                    new IncidentWorkerDto { WorkerId = company.Crew[3].Id, HoursUsed = 8 },
                ],
            },
            users.ProjectAdmin
        );
        await _incidents.ApproveAsync(incident.Id, users.OperationsDirector);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Demo data already present; skipping.")]
    private static partial void LogAlreadySeeded(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Demo data created ({Accounts} demo accounts). See README for the credentials."
    )]
    private static partial void LogSeeded(ILogger logger, int accounts);

    private sealed record DemoUsers(
        int GeneralManager,
        int OperationsDirector,
        int ProjectAdmin,
        int BranchAdmin,
        int BusinessManager
    );

    private sealed record DemoCompany(
        int Office,
        int Store,
        int Warehouse,
        List<int> Customers,
        int StoreCustomer,
        List<int> Suppliers,
        List<(int Id, decimal HourlyRate)> Crew,
        Dictionary<string, int> Materials
    );
}

public sealed record DemoAccount(string Email, string Name, string Role);
