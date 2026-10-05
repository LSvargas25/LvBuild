using FluentAssertions;
using LvApplication.DTOs.Commercial;
using LvDomain.Entities.Branches;
using LvDomain.Entities.Commercial;
using LvDomain.Enums;
using LvInfrastructure.Persistence;
using LvTest.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LvTest.Integration;

/// <summary>
/// Exercises invoice issuance with stock control against real PostgreSQL. This is
/// specifically the area that already bit the team once (a filtered unique index on
/// Invoice.InvoiceNumber behaves differently for NULL on a real database engine vs InMemory) -
/// the second test below is a direct regression test for that fix against the real engine.
/// </summary>
[Collection(PostgresIntegrationCollection.Name)]
public class InvoiceStockControlFlowTests : IAsyncLifetime
{
    private readonly PostgresFixture _db;

    public InvoiceStockControlFlowTests(PostgresFixture db)
    {
        _db = db;
    }

    public Task InitializeAsync() => _db.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task<(
        Branch Branch,
        CashRegister CashRegister,
        Product Product,
        LvDomain.Entities.Auth.User User
    )> SeedContextAsync(AppDbContext context, string emailPrefix)
    {
        var user = await TestUserFactory.CreateAsync(
            context,
            $"{emailPrefix}-{Guid.NewGuid():N}@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );

        var branch = new Branch
        {
            Name = "Comercio Integracion",
            City = "San Jose",
            Province = "San Jose",
            Status = BranchStatus.Active,
            BranchType = BranchType.Commercial,
            OperationsDirectorId = user.Id,
            CreatedAt = DateTime.UtcNow,
        };
        context.Branches.Add(branch);
        await context.SaveChangesAsync();

        var register = new CashRegister
        {
            BranchId = branch.Id,
            OpenedByUserId = user.Id,
            OpeningDate = DateTime.UtcNow,
            OpeningBalance = 0m,
            Status = CashRegisterStatus.Open,
            CreatedAt = DateTime.UtcNow,
        };
        context.CashRegisters.Add(register);

        var product = new Product
        {
            Name = "Producto Factura Integracion",
            Sku = $"SKU-INV-{Guid.NewGuid():N}"[..20],
            UnitPrice = 1000m,
            UnitCost = 700m,
            Status = ProductStatus.Validated,
            ActiveStatus = true,
            CreatedByUserId = user.Id,
            ValidatedByUserId = user.Id,
            ValidatedDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        context.BranchInventories.Add(
            new BranchInventory
            {
                BranchId = branch.Id,
                ProductId = product.Id,
                Quantity = 20m,
                MinimumStock = 0,
                CreatedAt = DateTime.UtcNow,
            }
        );
        await context.SaveChangesAsync();

        return (branch, register, product, user);
    }

    [Fact]
    public async Task IssueAsync_ValidDraft_DecrementsStockAndAssignsNumberAgainstRealPostgres()
    {
        await using var context = _db.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var (branch, register, product, user) = await SeedContextAsync(context, "inv1");
        var service = ServiceFactory.CreateInvoiceService(context);

        var draft = await service.CreateAsync(
            new CreateInvoiceDto
            {
                BranchId = branch.Id,
                CashRegisterId = register.Id,
                PaymentType = InvoicePaymentType.Credito,
                Details = new()
                {
                    new InvoiceDetailLineDto { ProductId = product.Id, Quantity = 5 },
                },
            },
            user.Id
        );

        var issued = await service.IssueAsync(draft.Id, new IssueInvoiceDto(), user.Id);

        issued.Status.Should().Be(InvoiceStatus.Issued);
        issued.InvoiceNumber.Should().NotBeNullOrEmpty();

        context.ChangeTracker.Clear();
        var inventory = await context
            .BranchInventories.AsNoTracking()
            .FirstAsync(i => i.BranchId == branch.Id && i.ProductId == product.Id);
        inventory.Quantity.Should().Be(15m);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task CreateAsync_TwoDraftInvoices_BothWithNullInvoiceNumber_DoNotViolateFilteredUniqueIndexOnRealPostgres()
    {
        await using var context = _db.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var (branch, register, product, user) = await SeedContextAsync(context, "inv2");
        var service = ServiceFactory.CreateInvoiceService(context);

        var draftDto = new CreateInvoiceDto
        {
            BranchId = branch.Id,
            CashRegisterId = register.Id,
            PaymentType = InvoicePaymentType.Credito,
            Details = new()
            {
                new InvoiceDetailLineDto { ProductId = product.Id, Quantity = 1 },
            },
        };

        var firstDraft = await service.CreateAsync(draftDto, user.Id);
        var secondDraft = await service.CreateAsync(draftDto, user.Id);

        firstDraft.InvoiceNumber.Should().BeNull();
        secondDraft.InvoiceNumber.Should().BeNull();

        context.ChangeTracker.Clear();
        var draftCount = await context
            .Invoices.AsNoTracking()
            .CountAsync(i => i.InvoiceNumber == null);
        draftCount.Should().BeGreaterThanOrEqualTo(2);

        await transaction.RollbackAsync();
    }
}
