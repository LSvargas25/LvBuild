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

public class InvoiceServiceTests
{
    private static async Task<Branch> CreateBranchAsync(AppDbContext context, int operationsDirectorId)
    {
        var branch = new Branch
        {
            Name = "Comercio San Jose",
            City = "San Jose",
            Province = "San Jose",
            Status = BranchStatus.Active,
            BranchType = BranchType.Commercial,
            OperationsDirectorId = operationsDirectorId,
            CreatedAt = DateTime.UtcNow
        };
        context.Branches.Add(branch);
        await context.SaveChangesAsync();
        return branch;
    }

    private static async Task<CashRegister> CreateOpenCashRegisterAsync(AppDbContext context, int branchId, int openedByUserId)
    {
        var register = new CashRegister
        {
            BranchId = branchId,
            OpenedByUserId = openedByUserId,
            OpeningDate = DateTime.UtcNow,
            OpeningBalance = 0m,
            Status = CashRegisterStatus.Open,
            CreatedAt = DateTime.UtcNow
        };
        context.CashRegisters.Add(register);
        await context.SaveChangesAsync();
        return register;
    }

    private static async Task<Product> CreateValidatedProductAsync(AppDbContext context, int createdByUserId, string sku, decimal unitPrice = 1000m)
    {
        var product = new Product
        {
            Name = "Producto " + sku,
            Sku = sku,
            UnitPrice = unitPrice,
            UnitCost = 700m,
            Status = ProductStatus.Validated,
            ActiveStatus = true,
            CreatedByUserId = createdByUserId,
            ValidatedByUserId = createdByUserId,
            ValidatedDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product;
    }

    private static async Task<BranchInventory> CreateInventoryAsync(AppDbContext context, int branchId, int productId, decimal quantity)
    {
        var inventory = new BranchInventory { BranchId = branchId, ProductId = productId, Quantity = quantity, MinimumStock = 0, CreatedAt = DateTime.UtcNow };
        context.BranchInventories.Add(inventory);
        await context.SaveChangesAsync();
        return inventory;
    }

    private static async Task<(Branch Branch, CashRegister CashRegister, int UserId)> CreateContextAsync(AppDbContext context, string emailPrefix)
    {
        var user = await TestUserFactory.CreateAsync(context, $"{emailPrefix}@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var branch = await CreateBranchAsync(context, user.Id);
        var register = await CreateOpenCashRegisterAsync(context, branch.Id, user.Id);
        return (branch, register, user.Id);
    }

    [Fact]
    public async Task CreateAsync_ResolvesUnitPriceFromProduct_AndComputesTotalsWithTax()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, register, userId) = await CreateContextAsync(context, "u1");
        var product = await CreateValidatedProductAsync(context, userId, "SKU-INV-1", 1000m);
        await CreateInventoryAsync(context, branch.Id, product.Id, 50m);
        var service = ServiceFactory.CreateInvoiceService(context);

        var result = await service.CreateAsync(new CreateInvoiceDto
        {
            BranchId = branch.Id,
            CashRegisterId = register.Id,
            PaymentType = InvoicePaymentType.Credito,
            Details = new() { new InvoiceDetailLineDto { ProductId = product.Id, Quantity = 3 } }
        }, userId);

        result.Status.Should().Be(InvoiceStatus.Draft);
        result.InvoiceNumber.Should().BeNull();
        result.Subtotal.Should().Be(3000m);
        result.Tax.Should().Be(390m);
        result.Total.Should().Be(3390m);
        result.Details.Single().UnitPrice.Should().Be(1000m);
    }

    [Fact]
    public async Task CreateAsync_NonValidatedProduct_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, register, userId) = await CreateContextAsync(context, "u2");
        var product = new Product
        {
            Name = "Pendiente",
            Sku = "SKU-INV-2",
            UnitPrice = 500m,
            UnitCost = 300m,
            Status = ProductStatus.PendingValidation,
            ActiveStatus = true,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var service = ServiceFactory.CreateInvoiceService(context);

        var act = () => service.CreateAsync(new CreateInvoiceDto
        {
            BranchId = branch.Id,
            CashRegisterId = register.Id,
            PaymentType = InvoicePaymentType.Credito,
            Details = new() { new InvoiceDetailLineDto { ProductId = product.Id, Quantity = 1 } }
        }, userId);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task CreateAsync_TwoDraftsInSameBranch_BothWithNullInvoiceNumber_DoesNotViolateUniqueIndex()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, register, userId) = await CreateContextAsync(context, "u16");
        var product = await CreateValidatedProductAsync(context, userId, "SKU-INV-16");
        await CreateInventoryAsync(context, branch.Id, product.Id, 10m);
        var service = ServiceFactory.CreateInvoiceService(context);

        var draft1 = await service.CreateAsync(new CreateInvoiceDto
        {
            BranchId = branch.Id,
            CashRegisterId = register.Id,
            PaymentType = InvoicePaymentType.Credito,
            Details = new() { new InvoiceDetailLineDto { ProductId = product.Id, Quantity = 1 } }
        }, userId);

        var draft2 = await service.CreateAsync(new CreateInvoiceDto
        {
            BranchId = branch.Id,
            CashRegisterId = register.Id,
            PaymentType = InvoicePaymentType.Credito,
            Details = new() { new InvoiceDetailLineDto { ProductId = product.Id, Quantity = 1 } }
        }, userId);

        draft1.InvoiceNumber.Should().BeNull();
        draft2.InvoiceNumber.Should().BeNull();
        draft1.Id.Should().NotBe(draft2.Id);
        (await context.Invoices.CountAsync(i => i.BranchId == branch.Id && i.Status == InvoiceStatus.Draft))
            .Should().Be(2);
    }

    [Fact]
    public async Task IssueAsync_SufficientStock_DecrementsInventoryAndAssignsNumber()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, register, userId) = await CreateContextAsync(context, "u3");
        var product = await CreateValidatedProductAsync(context, userId, "SKU-INV-3");
        await CreateInventoryAsync(context, branch.Id, product.Id, 20m);
        var service = ServiceFactory.CreateInvoiceService(context);
        var draft = await service.CreateAsync(new CreateInvoiceDto
        {
            BranchId = branch.Id,
            CashRegisterId = register.Id,
            PaymentType = InvoicePaymentType.Credito,
            Details = new() { new InvoiceDetailLineDto { ProductId = product.Id, Quantity = 5 } }
        }, userId);

        var issued = await service.IssueAsync(draft.Id, new IssueInvoiceDto(), userId);

        issued.Status.Should().Be(InvoiceStatus.Issued);
        issued.InvoiceNumber.Should().NotBeNullOrEmpty();
        var inventory = await context.BranchInventories.FirstAsync(i => i.BranchId == branch.Id && i.ProductId == product.Id);
        inventory.Quantity.Should().Be(15m);
    }

    [Fact]
    public async Task IssueAsync_InsufficientStockOnAnyLine_ThrowsAndDecrementsNothing()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, register, userId) = await CreateContextAsync(context, "u4");
        var productA = await CreateValidatedProductAsync(context, userId, "SKU-INV-4A");
        var productB = await CreateValidatedProductAsync(context, userId, "SKU-INV-4B");
        await CreateInventoryAsync(context, branch.Id, productA.Id, 10m);
        await CreateInventoryAsync(context, branch.Id, productB.Id, 1m);
        var service = ServiceFactory.CreateInvoiceService(context);
        var draft = await service.CreateAsync(new CreateInvoiceDto
        {
            BranchId = branch.Id,
            CashRegisterId = register.Id,
            PaymentType = InvoicePaymentType.Credito,
            Details = new()
            {
                new InvoiceDetailLineDto { ProductId = productA.Id, Quantity = 3 },
                new InvoiceDetailLineDto { ProductId = productB.Id, Quantity = 5 }
            }
        }, userId);

        var act = () => service.IssueAsync(draft.Id, new IssueInvoiceDto(), userId);

        await act.Should().ThrowAsync<ValidationAppException>();
        var inventoryA = await context.BranchInventories.FirstAsync(i => i.BranchId == branch.Id && i.ProductId == productA.Id);
        inventoryA.Quantity.Should().Be(10m);
    }

    [Fact]
    public async Task IssueAsync_CashRegisterNotOpen_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, register, userId) = await CreateContextAsync(context, "u5");
        var product = await CreateValidatedProductAsync(context, userId, "SKU-INV-5");
        await CreateInventoryAsync(context, branch.Id, product.Id, 10m);
        var invoiceService = ServiceFactory.CreateInvoiceService(context);
        var draft = await invoiceService.CreateAsync(new CreateInvoiceDto
        {
            BranchId = branch.Id,
            CashRegisterId = register.Id,
            PaymentType = InvoicePaymentType.Credito,
            Details = new() { new InvoiceDetailLineDto { ProductId = product.Id, Quantity = 2 } }
        }, userId);

        var cashRegisterService = ServiceFactory.CreateCashRegisterService(context);
        await cashRegisterService.CloseAsync(register.Id, new CloseCashRegisterDto { ClosingBalance = 0m }, userId);

        var act = () => invoiceService.IssueAsync(draft.Id, new IssueInvoiceDto(), userId);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task IssueAsync_ContadoWithPartialPayment_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, register, userId) = await CreateContextAsync(context, "u6");
        var product = await CreateValidatedProductAsync(context, userId, "SKU-INV-6");
        await CreateInventoryAsync(context, branch.Id, product.Id, 10m);
        var service = ServiceFactory.CreateInvoiceService(context);
        var draft = await service.CreateAsync(new CreateInvoiceDto
        {
            BranchId = branch.Id,
            CashRegisterId = register.Id,
            PaymentType = InvoicePaymentType.Contado,
            Details = new() { new InvoiceDetailLineDto { ProductId = product.Id, Quantity = 2 } }
        }, userId);

        var act = () => service.IssueAsync(draft.Id, new IssueInvoiceDto
        {
            Payments = new() { new CreateInvoicePaymentDto { PaymentMethod = InvoicePaymentMethod.Efectivo, Amount = 100m } }
        }, userId);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task IssueAsync_ContadoWithFullPayment_SucceedsAndIsFullyPaid()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, register, userId) = await CreateContextAsync(context, "u7");
        var product = await CreateValidatedProductAsync(context, userId, "SKU-INV-7");
        await CreateInventoryAsync(context, branch.Id, product.Id, 10m);
        var service = ServiceFactory.CreateInvoiceService(context);
        var draft = await service.CreateAsync(new CreateInvoiceDto
        {
            BranchId = branch.Id,
            CashRegisterId = register.Id,
            PaymentType = InvoicePaymentType.Contado,
            Details = new() { new InvoiceDetailLineDto { ProductId = product.Id, Quantity = 2 } }
        }, userId);

        var issued = await service.IssueAsync(draft.Id, new IssueInvoiceDto
        {
            Payments = new() { new CreateInvoicePaymentDto { PaymentMethod = InvoicePaymentMethod.Efectivo, Amount = draft.Total } }
        }, userId);

        issued.Status.Should().Be(InvoiceStatus.Issued);
        issued.IsFullyPaid.Should().BeTrue();
        issued.Balance.Should().Be(0m);
    }

    [Fact]
    public async Task IssueAsync_CreditoWithNoPayments_SucceedsWithFullBalance()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, register, userId) = await CreateContextAsync(context, "u8");
        var product = await CreateValidatedProductAsync(context, userId, "SKU-INV-8");
        await CreateInventoryAsync(context, branch.Id, product.Id, 10m);
        var service = ServiceFactory.CreateInvoiceService(context);
        var draft = await service.CreateAsync(new CreateInvoiceDto
        {
            BranchId = branch.Id,
            CashRegisterId = register.Id,
            PaymentType = InvoicePaymentType.Credito,
            Details = new() { new InvoiceDetailLineDto { ProductId = product.Id, Quantity = 2 } }
        }, userId);

        var issued = await service.IssueAsync(draft.Id, new IssueInvoiceDto(), userId);

        issued.Balance.Should().Be(issued.Total);
        issued.IsFullyPaid.Should().BeFalse();
    }

    [Fact]
    public async Task AddPaymentAsync_ReducesBalance_AndFullyPaysWhenSumEqualsTotal()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, register, userId) = await CreateContextAsync(context, "u9");
        var product = await CreateValidatedProductAsync(context, userId, "SKU-INV-9");
        await CreateInventoryAsync(context, branch.Id, product.Id, 10m);
        var service = ServiceFactory.CreateInvoiceService(context);
        var draft = await service.CreateAsync(new CreateInvoiceDto
        {
            BranchId = branch.Id,
            CashRegisterId = register.Id,
            PaymentType = InvoicePaymentType.Credito,
            Details = new() { new InvoiceDetailLineDto { ProductId = product.Id, Quantity = 2 } }
        }, userId);
        var issued = await service.IssueAsync(draft.Id, new IssueInvoiceDto(), userId);

        var half = issued.Total / 2;
        var afterFirst = await service.AddPaymentAsync(issued.Id, new CreateInvoicePaymentDto { PaymentMethod = InvoicePaymentMethod.Sinpe, Amount = half }, userId);
        afterFirst.IsFullyPaid.Should().BeFalse();

        var afterSecond = await service.AddPaymentAsync(issued.Id, new CreateInvoicePaymentDto { PaymentMethod = InvoicePaymentMethod.Sinpe, Amount = issued.Total - half }, userId);
        afterSecond.IsFullyPaid.Should().BeTrue();
        afterSecond.Balance.Should().Be(0m);
    }

    [Fact]
    public async Task AddPaymentAsync_AmountExceedsBalance_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, register, userId) = await CreateContextAsync(context, "u10");
        var product = await CreateValidatedProductAsync(context, userId, "SKU-INV-10");
        await CreateInventoryAsync(context, branch.Id, product.Id, 10m);
        var service = ServiceFactory.CreateInvoiceService(context);
        var draft = await service.CreateAsync(new CreateInvoiceDto
        {
            BranchId = branch.Id,
            CashRegisterId = register.Id,
            PaymentType = InvoicePaymentType.Credito,
            Details = new() { new InvoiceDetailLineDto { ProductId = product.Id, Quantity = 2 } }
        }, userId);
        var issued = await service.IssueAsync(draft.Id, new IssueInvoiceDto(), userId);

        var act = () => service.AddPaymentAsync(issued.Id, new CreateInvoicePaymentDto { PaymentMethod = InvoicePaymentMethod.Tarjeta, Amount = issued.Total + 1m }, userId);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task CancelAsync_CreditoWithNoPayments_RevertsStock()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, register, userId) = await CreateContextAsync(context, "u11");
        var product = await CreateValidatedProductAsync(context, userId, "SKU-INV-11");
        await CreateInventoryAsync(context, branch.Id, product.Id, 10m);
        var service = ServiceFactory.CreateInvoiceService(context);
        var draft = await service.CreateAsync(new CreateInvoiceDto
        {
            BranchId = branch.Id,
            CashRegisterId = register.Id,
            PaymentType = InvoicePaymentType.Credito,
            Details = new() { new InvoiceDetailLineDto { ProductId = product.Id, Quantity = 4 } }
        }, userId);
        var issued = await service.IssueAsync(draft.Id, new IssueInvoiceDto(), userId);

        var cancelled = await service.CancelAsync(issued.Id);

        cancelled.Status.Should().Be(InvoiceStatus.Cancelled);
        var inventory = await context.BranchInventories.FirstAsync(i => i.BranchId == branch.Id && i.ProductId == product.Id);
        inventory.Quantity.Should().Be(10m);
    }

    [Fact]
    public async Task CancelAsync_CreditoWithPayments_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, register, userId) = await CreateContextAsync(context, "u12");
        var product = await CreateValidatedProductAsync(context, userId, "SKU-INV-12");
        await CreateInventoryAsync(context, branch.Id, product.Id, 10m);
        var service = ServiceFactory.CreateInvoiceService(context);
        var draft = await service.CreateAsync(new CreateInvoiceDto
        {
            BranchId = branch.Id,
            CashRegisterId = register.Id,
            PaymentType = InvoicePaymentType.Credito,
            Details = new() { new InvoiceDetailLineDto { ProductId = product.Id, Quantity = 2 } }
        }, userId);
        var issued = await service.IssueAsync(draft.Id, new IssueInvoiceDto(), userId);
        await service.AddPaymentAsync(issued.Id, new CreateInvoicePaymentDto { PaymentMethod = InvoicePaymentMethod.Sinpe, Amount = 10m }, userId);

        var act = () => service.CancelAsync(issued.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task CancelAsync_ContadoInvoice_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, register, userId) = await CreateContextAsync(context, "u13");
        var product = await CreateValidatedProductAsync(context, userId, "SKU-INV-13");
        await CreateInventoryAsync(context, branch.Id, product.Id, 10m);
        var service = ServiceFactory.CreateInvoiceService(context);
        var draft = await service.CreateAsync(new CreateInvoiceDto
        {
            BranchId = branch.Id,
            CashRegisterId = register.Id,
            PaymentType = InvoicePaymentType.Contado,
            Details = new() { new InvoiceDetailLineDto { ProductId = product.Id, Quantity = 2 } }
        }, userId);
        var issued = await service.IssueAsync(draft.Id, new IssueInvoiceDto
        {
            Payments = new() { new CreateInvoicePaymentDto { PaymentMethod = InvoicePaymentMethod.Efectivo, Amount = draft.Total } }
        }, userId);

        var act = () => service.CancelAsync(issued.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task DeleteAsync_IssuedInvoice_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, register, userId) = await CreateContextAsync(context, "u14");
        var product = await CreateValidatedProductAsync(context, userId, "SKU-INV-14");
        await CreateInventoryAsync(context, branch.Id, product.Id, 10m);
        var service = ServiceFactory.CreateInvoiceService(context);
        var draft = await service.CreateAsync(new CreateInvoiceDto
        {
            BranchId = branch.Id,
            CashRegisterId = register.Id,
            PaymentType = InvoicePaymentType.Credito,
            Details = new() { new InvoiceDetailLineDto { ProductId = product.Id, Quantity = 2 } }
        }, userId);
        var issued = await service.IssueAsync(draft.Id, new IssueInvoiceDto(), userId);

        var act = () => service.DeleteAsync(issued.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task DeleteAsync_DraftInvoice_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, register, userId) = await CreateContextAsync(context, "u15");
        var product = await CreateValidatedProductAsync(context, userId, "SKU-INV-15");
        await CreateInventoryAsync(context, branch.Id, product.Id, 10m);
        var service = ServiceFactory.CreateInvoiceService(context);
        var draft = await service.CreateAsync(new CreateInvoiceDto
        {
            BranchId = branch.Id,
            CashRegisterId = register.Id,
            PaymentType = InvoicePaymentType.Credito,
            Details = new() { new InvoiceDetailLineDto { ProductId = product.Id, Quantity = 2 } }
        }, userId);

        await service.DeleteAsync(draft.Id);

        (await context.Invoices.FindAsync(draft.Id)).Should().BeNull();
    }
}
