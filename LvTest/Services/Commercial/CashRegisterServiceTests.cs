using FluentAssertions;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Commercial;
using LvDomain.Entities.Branches;
using LvDomain.Entities.Commercial;
using LvDomain.Enums;
using LvInfrastructure.Persistence;
using LvTest.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LvTest.Services.Commercial;

public class CashRegisterServiceTests
{
    private static async Task<Branch> CreateBranchAsync(
        AppDbContext context,
        int operationsDirectorId
    )
    {
        var branch = new Branch
        {
            Name = "Comercio San Jose",
            City = "San Jose",
            Province = "San Jose",
            Status = BranchStatus.Active,
            BranchType = BranchType.Commercial,
            OperationsDirectorId = operationsDirectorId,
            CreatedAt = DateTime.UtcNow,
        };
        context.Branches.Add(branch);
        await context.SaveChangesAsync();
        return branch;
    }

    private static async Task<Product> CreateValidatedProductAsync(
        AppDbContext context,
        int createdByUserId,
        string sku,
        decimal unitPrice = 1000m
    )
    {
        var product = new Product
        {
            Name = "Producto",
            Sku = sku,
            UnitPrice = unitPrice,
            UnitCost = 700m,
            Status = ProductStatus.Validated,
            ActiveStatus = true,
            CreatedByUserId = createdByUserId,
            ValidatedByUserId = createdByUserId,
            ValidatedDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product;
    }

    [Fact]
    public async Task OpenAsync_BranchNotCommercial_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "gmwh@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var warehouseBranch = new Branch
        {
            Name = "Bodega Central",
            City = "San Jose",
            Province = "San Jose",
            Status = BranchStatus.Active,
            BranchType = BranchType.Warehouse,
            OperationsDirectorId = user.Id,
            CreatedAt = DateTime.UtcNow,
        };
        context.Branches.Add(warehouseBranch);
        await context.SaveChangesAsync();
        var service = ServiceFactory.CreateCashRegisterService(context);

        var act = () =>
            service.OpenAsync(
                new OpenCashRegisterDto { BranchId = warehouseBranch.Id, OpeningBalance = 0m },
                user.Id
            );

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task OpenAsync_CreatesRegisterInOpenStatus()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "gm@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var branch = await CreateBranchAsync(context, user.Id);
        var service = ServiceFactory.CreateCashRegisterService(context);

        var result = await service.OpenAsync(
            new OpenCashRegisterDto { BranchId = branch.Id, OpeningBalance = 500m },
            user.Id
        );

        result.Status.Should().Be(CashRegisterStatus.Open);
        result.OpeningBalance.Should().Be(500m);
    }

    [Fact]
    public async Task OpenAsync_WhenBranchAlreadyHasOpenRegister_ThrowsConflict()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "gm2@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var branch = await CreateBranchAsync(context, user.Id);
        var service = ServiceFactory.CreateCashRegisterService(context);
        await service.OpenAsync(
            new OpenCashRegisterDto { BranchId = branch.Id, OpeningBalance = 100m },
            user.Id
        );

        var act = () =>
            service.OpenAsync(
                new OpenCashRegisterDto { BranchId = branch.Id, OpeningBalance = 200m },
                user.Id
            );

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CloseAsync_WithNoInvoices_ExpectedBalanceEqualsOpeningBalance()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "gm3@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var branch = await CreateBranchAsync(context, user.Id);
        var service = ServiceFactory.CreateCashRegisterService(context);
        var opened = await service.OpenAsync(
            new OpenCashRegisterDto { BranchId = branch.Id, OpeningBalance = 500m },
            user.Id
        );

        var closed = await service.CloseAsync(
            opened.Id,
            new CloseCashRegisterDto { ClosingBalance = 500m },
            user.Id
        );

        closed.Status.Should().Be(CashRegisterStatus.Closed);
        closed.ExpectedBalance.Should().Be(500m);
        closed.Difference.Should().Be(0m);
    }

    [Fact]
    public async Task CloseAsync_CountsOnlyEfectivoPayments()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "gm4@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var branch = await CreateBranchAsync(context, user.Id);
        var product = await CreateValidatedProductAsync(context, user.Id, "SKU-CR-1", 1000m);
        var cashRegisterService = ServiceFactory.CreateCashRegisterService(context);
        var invoiceService = ServiceFactory.CreateInvoiceService(context);
        var opened = await cashRegisterService.OpenAsync(
            new OpenCashRegisterDto { BranchId = branch.Id, OpeningBalance = 500m },
            user.Id
        );

        context.BranchInventories.Add(
            new BranchInventory
            {
                BranchId = branch.Id,
                ProductId = product.Id,
                Quantity = 100m,
                MinimumStock = 0,
                CreatedAt = DateTime.UtcNow,
            }
        );
        await context.SaveChangesAsync();

        var draft = await invoiceService.CreateAsync(
            new CreateInvoiceDto
            {
                BranchId = branch.Id,
                CashRegisterId = opened.Id,
                PaymentType = InvoicePaymentType.Credito,
                Details = new()
                {
                    new InvoiceDetailLineDto { ProductId = product.Id, Quantity = 2 },
                },
            },
            user.Id
        );

        var issued = await invoiceService.IssueAsync(draft.Id, new IssueInvoiceDto(), user.Id);
        await invoiceService.AddPaymentAsync(
            issued.Id,
            new CreateInvoicePaymentDto
            {
                PaymentMethod = InvoicePaymentMethod.Efectivo,
                Amount = 1000m,
            },
            user.Id
        );
        await invoiceService.AddPaymentAsync(
            issued.Id,
            new CreateInvoicePaymentDto
            {
                PaymentMethod = InvoicePaymentMethod.Tarjeta,
                Amount = issued.Total - 1000m,
            },
            user.Id
        );

        var closed = await cashRegisterService.CloseAsync(
            opened.Id,
            new CloseCashRegisterDto { ClosingBalance = 1500m },
            user.Id
        );

        closed.ExpectedBalance.Should().Be(1500m);
    }

    [Fact]
    public async Task CloseAsync_AlreadyClosed_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "gm5@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var branch = await CreateBranchAsync(context, user.Id);
        var service = ServiceFactory.CreateCashRegisterService(context);
        var opened = await service.OpenAsync(
            new OpenCashRegisterDto { BranchId = branch.Id, OpeningBalance = 500m },
            user.Id
        );
        await service.CloseAsync(
            opened.Id,
            new CloseCashRegisterDto { ClosingBalance = 500m },
            user.Id
        );

        var act = () =>
            service.CloseAsync(
                opened.Id,
                new CloseCashRegisterDto { ClosingBalance = 500m },
                user.Id
            );

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task DeleteAsync_OpenWithNoInvoices_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "gm6@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var branch = await CreateBranchAsync(context, user.Id);
        var service = ServiceFactory.CreateCashRegisterService(context);
        var opened = await service.OpenAsync(
            new OpenCashRegisterDto { BranchId = branch.Id, OpeningBalance = 500m },
            user.Id
        );

        await service.DeleteAsync(opened.Id);

        (await context.CashRegisters.FindAsync(opened.Id)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_OpenWithInvoices_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "gm7@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var branch = await CreateBranchAsync(context, user.Id);
        var product = await CreateValidatedProductAsync(context, user.Id, "SKU-CR-2");
        var cashRegisterService = ServiceFactory.CreateCashRegisterService(context);
        var invoiceService = ServiceFactory.CreateInvoiceService(context);
        var opened = await cashRegisterService.OpenAsync(
            new OpenCashRegisterDto { BranchId = branch.Id, OpeningBalance = 500m },
            user.Id
        );

        await invoiceService.CreateAsync(
            new CreateInvoiceDto
            {
                BranchId = branch.Id,
                CashRegisterId = opened.Id,
                PaymentType = InvoicePaymentType.Credito,
                Details = new()
                {
                    new InvoiceDetailLineDto { ProductId = product.Id, Quantity = 1 },
                },
            },
            user.Id
        );

        var act = () => cashRegisterService.DeleteAsync(opened.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task DeleteAsync_ClosedRegister_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "gm8@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var branch = await CreateBranchAsync(context, user.Id);
        var service = ServiceFactory.CreateCashRegisterService(context);
        var opened = await service.OpenAsync(
            new OpenCashRegisterDto { BranchId = branch.Id, OpeningBalance = 500m },
            user.Id
        );
        await service.CloseAsync(
            opened.Id,
            new CloseCashRegisterDto { ClosingBalance = 500m },
            user.Id
        );

        var act = () => service.DeleteAsync(opened.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }
}
