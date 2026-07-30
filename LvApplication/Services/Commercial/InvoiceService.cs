using FluentValidation;
using LvApplication.Common;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Commercial;
using LvDomain.Entities.Commercial;
using LvDomain.Enums;

namespace LvApplication.Services.Commercial;

public class InvoiceService : IInvoiceService
{
    private const decimal TaxRate = 0.13m;

    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IBranchInventoryRepository _inventoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICashRegisterRepository _cashRegisterRepository;
    private readonly IValidator<CreateInvoiceDto> _createValidator;
    private readonly IValidator<UpdateInvoiceDraftDto> _updateDraftValidator;
    private readonly IValidator<IssueInvoiceDto> _issueValidator;
    private readonly IValidator<CreateInvoicePaymentDto> _paymentValidator;

    public InvoiceService(
        IInvoiceRepository invoiceRepository,
        IBranchInventoryRepository inventoryRepository,
        IProductRepository productRepository,
        ICashRegisterRepository cashRegisterRepository,
        IValidator<CreateInvoiceDto> createValidator,
        IValidator<UpdateInvoiceDraftDto> updateDraftValidator,
        IValidator<IssueInvoiceDto> issueValidator,
        IValidator<CreateInvoicePaymentDto> paymentValidator)
    {
        _invoiceRepository = invoiceRepository;
        _inventoryRepository = inventoryRepository;
        _productRepository = productRepository;
        _cashRegisterRepository = cashRegisterRepository;
        _createValidator = createValidator;
        _updateDraftValidator = updateDraftValidator;
        _issueValidator = issueValidator;
        _paymentValidator = paymentValidator;
    }

    public async Task<InvoiceDto> CreateAsync(CreateInvoiceDto request, int createdByUserId)
    {
        await _createValidator.ValidateAndThrowAppExceptionAsync(request);

        var cashRegister = await _cashRegisterRepository.GetByIdAsync(request.CashRegisterId)
            ?? throw new NotFoundException($"CashRegister {request.CashRegisterId} not found.");

        if (cashRegister.BranchId != request.BranchId)
        {
            throw new ValidationAppException("La caja indicada no pertenece a la sucursal de la factura.");
        }

        var details = await BuildDetailLinesAsync(request.Details);
        var (subtotal, tax, total) = CalculateTotals(details);

        var now = DateTime.UtcNow;
        var invoice = new Invoice
        {
            BranchId = request.BranchId,
            CashRegisterId = request.CashRegisterId,
            CustomerId = request.CustomerId,
            InvoiceNumber = null,
            Date = now,
            PaymentType = request.PaymentType,
            Status = InvoiceStatus.Draft,
            Subtotal = subtotal,
            Tax = tax,
            Total = total,
            CreatedByUserId = createdByUserId,
            Details = details,
            CreatedAt = now
        };

        await _invoiceRepository.AddAsync(invoice);

        return MapToDto(invoice);
    }

    public async Task<InvoiceDto> UpdateDraftAsync(int id, UpdateInvoiceDraftDto request)
    {
        await _updateDraftValidator.ValidateAndThrowAppExceptionAsync(request);

        var invoice = await _invoiceRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Invoice {id} not found.");

        if (invoice.Status != InvoiceStatus.Draft)
        {
            throw new ValidationAppException("Solo se puede editar una factura en estado Draft.");
        }

        var details = await BuildDetailLinesAsync(request.Details);
        var (subtotal, tax, total) = CalculateTotals(details);

        invoice.CustomerId = request.CustomerId;
        invoice.PaymentType = request.PaymentType;
        invoice.Details.Clear();
        foreach (var detail in details)
        {
            invoice.Details.Add(detail);
        }
        invoice.Subtotal = subtotal;
        invoice.Tax = tax;
        invoice.Total = total;
        invoice.UpdatedAt = DateTime.UtcNow;

        await _invoiceRepository.UpdateAsync(invoice);

        return MapToDto(invoice);
    }

    public async Task<InvoiceDto> IssueAsync(int id, IssueInvoiceDto request, int actingUserId)
    {
        await _issueValidator.ValidateAndThrowAppExceptionAsync(request);

        var invoice = await _invoiceRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Invoice {id} not found.");

        if (invoice.Status != InvoiceStatus.Draft)
        {
            throw new ValidationAppException("Solo se puede emitir una factura en estado Draft.");
        }

        if (invoice.Details.Count == 0)
        {
            throw new ValidationAppException("La factura debe tener al menos una línea antes de emitirse.");
        }

        var cashRegister = await _cashRegisterRepository.GetByIdAsync(invoice.CashRegisterId)
            ?? throw new NotFoundException($"CashRegister {invoice.CashRegisterId} not found.");

        if (cashRegister.Status != CashRegisterStatus.Open)
        {
            throw new ValidationAppException("La caja asociada a la factura debe estar abierta para poder emitirla.");
        }

        var inventoryByProduct = new Dictionary<int, BranchInventory>();
        foreach (var detail in invoice.Details)
        {
            var inventory = await _inventoryRepository.GetByBranchAndProductAsync(invoice.BranchId, detail.ProductId);
            if (inventory is null || inventory.Quantity < detail.Quantity)
            {
                throw new ValidationAppException($"Stock insuficiente para el producto {detail.ProductId} en la sucursal {invoice.BranchId}.");
            }

            inventoryByProduct[detail.ProductId] = inventory;
        }

        if (invoice.PaymentType == InvoicePaymentType.Contado)
        {
            var paymentsSum = request.Payments.Sum(p => p.Amount);
            if (paymentsSum != invoice.Total)
            {
                throw new ValidationAppException("Para ventas de Contado, los pagos deben cubrir exactamente el total de la factura.");
            }
        }

        foreach (var detail in invoice.Details)
        {
            var inventory = inventoryByProduct[detail.ProductId];
            inventory.Quantity -= detail.Quantity;
            inventory.UpdatedAt = DateTime.UtcNow;
            await _inventoryRepository.UpdateAsync(inventory);
        }

        var nextNumber = await _invoiceRepository.CountByBranchAsync(invoice.BranchId) + 1;
        invoice.InvoiceNumber = nextNumber.ToString("D6");
        invoice.Status = InvoiceStatus.Issued;
        invoice.UpdatedAt = DateTime.UtcNow;

        var now = DateTime.UtcNow;
        foreach (var payment in request.Payments)
        {
            invoice.Payments.Add(new InvoicePayment
            {
                Date = now,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                ReceivedByUserId = actingUserId,
                CreatedAt = now
            });
        }

        await _invoiceRepository.UpdateAsync(invoice);

        return MapToDto(invoice);
    }

    public async Task<InvoiceDto> AddPaymentAsync(int id, CreateInvoicePaymentDto request, int receivedByUserId)
    {
        await _paymentValidator.ValidateAndThrowAppExceptionAsync(request);

        var invoice = await _invoiceRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Invoice {id} not found.");

        if (invoice.Status != InvoiceStatus.Issued || invoice.PaymentType != InvoicePaymentType.Credito)
        {
            throw new ValidationAppException("Solo se pueden registrar pagos sobre facturas emitidas de Crédito.");
        }

        var balance = invoice.Total - invoice.Payments.Sum(p => p.Amount);
        if (balance <= 0)
        {
            throw new ValidationAppException("La factura ya está completamente pagada.");
        }

        if (request.Amount > balance)
        {
            throw new ValidationAppException("El monto del pago no puede superar el saldo pendiente.");
        }

        invoice.Payments.Add(new InvoicePayment
        {
            Date = DateTime.UtcNow,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            ReceivedByUserId = receivedByUserId,
            CreatedAt = DateTime.UtcNow
        });
        invoice.UpdatedAt = DateTime.UtcNow;

        await _invoiceRepository.UpdateAsync(invoice);

        return MapToDto(invoice);
    }

    public async Task<InvoiceDto> CancelAsync(int id)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Invoice {id} not found.");

        if (invoice.Status != InvoiceStatus.Issued)
        {
            throw new ValidationAppException("Solo se puede cancelar una factura en estado Issued.");
        }

        if (invoice.PaymentType != InvoicePaymentType.Credito)
        {
            throw new ValidationAppException("Solo se pueden cancelar facturas de Crédito.");
        }

        if (invoice.Payments.Count > 0)
        {
            throw new ValidationAppException("No se puede cancelar una factura que ya tiene pagos registrados.");
        }

        foreach (var detail in invoice.Details)
        {
            var inventory = await _inventoryRepository.GetByBranchAndProductAsync(invoice.BranchId, detail.ProductId);
            if (inventory is not null)
            {
                inventory.Quantity += detail.Quantity;
                inventory.UpdatedAt = DateTime.UtcNow;
                await _inventoryRepository.UpdateAsync(inventory);
            }
        }

        invoice.Status = InvoiceStatus.Cancelled;
        invoice.UpdatedAt = DateTime.UtcNow;

        await _invoiceRepository.UpdateAsync(invoice);

        return MapToDto(invoice);
    }

    public async Task DeleteAsync(int id)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Invoice {id} not found.");

        if (invoice.Status != InvoiceStatus.Draft)
        {
            throw new ValidationAppException("Solo se puede eliminar una factura en estado Draft.");
        }

        await _invoiceRepository.DeleteAsync(invoice);
    }

    public async Task<InvoiceDto> GetByIdAsync(int id)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Invoice {id} not found.");
        return MapToDto(invoice);
    }

    public async Task<PagedResult<InvoiceDto>> GetAllByBranchAsync(int branchId, int pageNumber, int pageSize)
    {
        var (items, totalCount) = await _invoiceRepository.GetPagedByBranchAsync(branchId, pageNumber, pageSize);

        return new PagedResult<InvoiceDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    private async Task<List<InvoiceDetail>> BuildDetailLinesAsync(List<InvoiceDetailLineDto> lines)
    {
        var details = new List<InvoiceDetail>();

        foreach (var line in lines)
        {
            var product = await _productRepository.GetByIdAsync(line.ProductId)
                ?? throw new NotFoundException($"Product {line.ProductId} not found.");

            if (product.Status != ProductStatus.Validated)
            {
                throw new ValidationAppException($"El producto {product.Sku} no está validado y no puede facturarse.");
            }

            var subtotal = line.Quantity * product.UnitPrice;

            details.Add(new InvoiceDetail
            {
                ProductId = product.Id,
                Quantity = line.Quantity,
                UnitPrice = product.UnitPrice,
                Subtotal = subtotal,
                CreatedAt = DateTime.UtcNow
            });
        }

        return details;
    }

    private static (decimal Subtotal, decimal Tax, decimal Total) CalculateTotals(List<InvoiceDetail> details)
    {
        var subtotal = details.Sum(d => d.Subtotal);
        var tax = Math.Round(subtotal * TaxRate, 2);
        var total = subtotal + tax;
        return (subtotal, tax, total);
    }

    private static InvoiceDto MapToDto(Invoice invoice)
    {
        var totalPaid = invoice.Payments.Sum(p => p.Amount);

        return new InvoiceDto
        {
            Id = invoice.Id,
            BranchId = invoice.BranchId,
            CashRegisterId = invoice.CashRegisterId,
            CustomerId = invoice.CustomerId,
            InvoiceNumber = invoice.InvoiceNumber,
            Date = invoice.Date,
            PaymentType = invoice.PaymentType,
            Status = invoice.Status,
            Subtotal = invoice.Subtotal,
            Tax = invoice.Tax,
            Total = invoice.Total,
            CreatedByUserId = invoice.CreatedByUserId,
            Details = invoice.Details.Select(d => new InvoiceDetailDto
            {
                Id = d.Id,
                ProductId = d.ProductId,
                ProductName = d.Product?.Name ?? string.Empty,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                Subtotal = d.Subtotal
            }).ToList(),
            Payments = invoice.Payments.Select(p => new InvoicePaymentDto
            {
                Id = p.Id,
                Date = p.Date,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                ReceivedByUserId = p.ReceivedByUserId
            }).ToList(),
            TotalPaid = totalPaid,
            Balance = invoice.Total - totalPaid,
            IsFullyPaid = totalPaid >= invoice.Total
        };
    }
}
