using LvApplication.DTOs.Incidents;
using LvApplication.DTOs.Inventory;
using LvApplication.DTOs.Payroll;
using LvApplication.DTOs.Projects;
using LvApplication.DTOs.SiteLogs;
using LvDomain.Entities.Branches;
using LvDomain.Entities.Budgets;
using LvDomain.Entities.Customers;
using LvDomain.Entities.Materials;
using LvDomain.Entities.Offers;
using LvDomain.Entities.Suppliers;
using LvDomain.Entities.Workers;
using LvDomain.Enums;
using LvInfrastructure.Persistence;

namespace LvTest.Common;

/// <summary>
/// A project with costs in two months, built through the real services (shared by the InMemory
/// and PostgreSQL finance tests). The services stamp "now", so the dates are moved afterwards:
///
/// September 2026: payroll ₡8 000 paid 30/09 21:00 CR (= 01/10 03:00 UTC), ticket ₡5 000 applied,
///                 incident ₡3 000 approved. Direct ₡16 000, pending ₡0.
/// October 2026:   payroll ₡10 000 paid, ticket ₡4 000 applied, ticket ₡600 in review,
///                 incident ₡2 000 not approved. Direct ₡14 000, pending ₡2 600.
/// </summary>
public static class FinancePeriodScenario
{
    public const decimal SeptemberDirect = 16_000m;
    public const decimal SeptemberPayroll = 8_000m;
    public const decimal SeptemberMaterials = 5_000m;
    public const decimal SeptemberIncidents = 3_000m;
    public const decimal OctoberDirect = 14_000m;
    public const decimal OctoberPayroll = 10_000m;
    public const decimal OctoberMaterials = 4_000m;
    public const decimal OctoberPending = 2_600m;

    private static DateTime Utc(int year, int month, int day, int hour, int minute, int second) =>
        new(year, month, day, hour, minute, second, DateTimeKind.Utc);

    public static async Task<int> BuildAsync(AppDbContext context)
    {
        var director = await TestUserFactory.CreateAsync(
            context,
            $"director-{Guid.NewGuid():N}@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var manager = await TestUserFactory.CreateAsync(
            context,
            $"gm-{Guid.NewGuid():N}@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var admin = await TestUserFactory.CreateAsync(
            context,
            $"pa-{Guid.NewGuid():N}@example.com",
            roleId: TestUserFactory.ProjectAdminRoleId
        );

        var customer = new Customer
        {
            Name = "Cliente Finanzas",
            CustomerType = CustomerType.Project,
            Status = ActiveStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };
        var branch = new Branch
        {
            Name = "Oficina Finanzas",
            City = "San Jose",
            Province = "San Jose",
            Status = BranchStatus.Active,
            BranchType = BranchType.Office,
            OperationsDirectorId = director.Id,
            CreatedAt = DateTime.UtcNow,
        };
        var supplier = new Supplier
        {
            Name = "Proveedor Finanzas",
            Status = ActiveStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };
        var material = new MaterialCatalog { Name = "Cemento", CreatedAt = DateTime.UtcNow };
        var worker = new Worker
        {
            Name = "Trabajador Finanzas",
            Status = ActiveStatus.Active,
            Category = WorkerCategory.Construction,
            Type = WorkerType.Laborer,
            HourlyRate = 1_000m,
            CreatedAt = DateTime.UtcNow,
        };
        context.AddRange(customer, branch, supplier, material, worker);
        await context.SaveChangesAsync();

        var budget = new Budget
        {
            CustomerId = customer.Id,
            BranchId = branch.Id,
            Name = "Edificio Finanzas",
            Status = BudgetStatus.ClientApproved,
            UtilityPercentage = 10,
            IndirectCostsTotal = 50,
            TotalBudget = 1000,
            CreatedByUserId = manager.Id,
            CreatedAt = DateTime.UtcNow,
        };
        context.Budgets.Add(budget);
        await context.SaveChangesAsync();

        var offer = new Offer
        {
            BudgetId = budget.Id,
            CustomerId = customer.Id,
            OfferNumber = $"OF-FIN-{Guid.NewGuid():N}"[..20],
            OfferType = OfferType.Turnkey,
            IssueDate = new DateTime(2026, 8, 1),
            ValidityDays = 30,
            WorkLocation = "San Jose Centro",
            WorkScope = "Construccion de edificio",
            EstimatedStartDate = new DateTime(2026, 9, 1),
            EstimatedDurationWeeks = 10,
            EstimatedDeliveryDate = new DateTime(2026, 9, 1).AddDays(10 * 7),
            PaymentTerms = "50% inicio, 50% entrega",
            Warranties = "1 ano estructural",
            Exclusions = "No incluye mobiliario",
            TotalProjectPrice = 100_000m,
            Status = OfferStatus.ClientAccepted,
            CreatedByUserId = manager.Id,
            CreatedAt = DateTime.UtcNow,
        };
        context.Offers.Add(offer);
        await context.SaveChangesAsync();

        var project = await ServiceFactory
            .CreateProjectService(context)
            .CreateProjectAsync(
                new CreateProjectDto
                {
                    OfferId = offer.Id,
                    BranchId = branch.Id,
                    StartDate = new DateTime(2026, 9, 1),
                },
                manager.Id
            );

        // ---- payrolls (one approved site log each), paid then re-dated
        var siteLogs = ServiceFactory.CreateSiteLogService(context);
        var payrolls = ServiceFactory.CreatePayrollService(context);
        async Task PaidPayrollAsync(DateTime weekStart, decimal hours, DateTime paidAtUtc)
        {
            var siteLog = await siteLogs.CreateAsync(
                new CreateSiteLogDto
                {
                    ProjectId = project.Id,
                    WeekStart = weekStart,
                    WeekEnd = weekStart.AddDays(6),
                    TaskDescription = "Semana de trabajo",
                },
                admin.Id
            );
            await siteLogs.SubmitToReviewAsync(siteLog.Id);
            await siteLogs.ApproveAsync(siteLog.Id, manager.Id);
            var payroll = await payrolls.CreateAsync(
                new CreatePayrollDto
                {
                    SiteLogId = siteLog.Id,
                    Details =
                    [
                        new PayrollDetailDto
                        {
                            WorkerId = worker.Id,
                            Date = weekStart,
                            HoursWorked = hours,
                            HourlyRate = worker.HourlyRate,
                            PaymentType = PayrollPaymentType.Full,
                            Payments =
                            [
                                new PayrollDetailPaymentDto
                                {
                                    PaymentMethod = PaymentMethod.Transfer,
                                    Amount = hours * worker.HourlyRate,
                                },
                            ],
                        },
                    ],
                },
                admin.Id
            );
            await payrolls.MarkAsPaidAsync(payroll.Id);
            (await context.Payrolls.FindAsync(payroll.Id))!.PaidAt = paidAtUtc;
        }

        // Paid on the evening of 30/09 in Costa Rica, already 01/10 in UTC: belongs to September.
        await PaidPayrollAsync(new DateTime(2026, 9, 21), 8, Utc(2026, 10, 1, 3, 0, 0));
        await PaidPayrollAsync(new DateTime(2026, 10, 5), 10, Utc(2026, 10, 9, 21, 0, 0));

        // ---- material tickets
        var tickets = ServiceFactory.CreateMaterialTicketService(context);
        async Task TicketAsync(
            decimal quantity,
            decimal unitPrice,
            DateTime createdAtUtc,
            bool apply
        )
        {
            var ticket = await tickets.CreateAsync(
                project.Id,
                new CreateMaterialTicketDto
                {
                    SupplierId = supplier.Id,
                    MaterialId = material.Id,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                },
                admin.Id
            );
            if (apply)
                await tickets.ApplyAsync(ticket.Id);
            (await context.MaterialTickets.FindAsync(ticket.Id))!.CreatedAt = createdAtUtc;
        }

        await TicketAsync(10, 500, Utc(2026, 9, 10, 15, 0, 0), apply: true);
        await TicketAsync(4, 1_000, Utc(2026, 10, 5, 15, 0, 0), apply: true);
        await TicketAsync(2, 300, Utc(2026, 10, 6, 15, 0, 0), apply: false);

        // ---- incidents (worker hours × hourly rate)
        var incidents = ServiceFactory.CreateIncidentService(context);
        var approved = await incidents.CreateAsync(
            new CreateIncidentDto
            {
                ProjectId = project.Id,
                Date = new DateTime(2026, 9, 15),
                Description = "Lluvia dañó formaleta",
                Workers = [new IncidentWorkerDto { WorkerId = worker.Id, HoursUsed = 3 }],
            },
            admin.Id
        );
        await incidents.ApproveAsync(approved.Id, manager.Id);
        await incidents.CreateAsync(
            new CreateIncidentDto
            {
                ProjectId = project.Id,
                Date = new DateTime(2026, 10, 7),
                Description = "Reparación pendiente de aprobar",
                Workers = [new IncidentWorkerDto { WorkerId = worker.Id, HoursUsed = 2 }],
            },
            admin.Id
        );

        await context.SaveChangesAsync();
        return project.Id;
    }
}
