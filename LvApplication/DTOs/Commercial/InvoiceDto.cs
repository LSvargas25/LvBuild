using LvDomain.Enums;

namespace LvApplication.DTOs.Commercial;

public class InvoiceDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int CashRegisterId { get; set; }
    public int? CustomerId { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime Date { get; set; }
    public InvoicePaymentType PaymentType { get; set; }
    public InvoiceStatus Status { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public int CreatedByUserId { get; set; }
    public List<InvoiceDetailDto> Details { get; set; } = new();
    public List<InvoicePaymentDto> Payments { get; set; } = new();
    public decimal TotalPaid { get; set; }
    public decimal Balance { get; set; }
    public bool IsFullyPaid { get; set; }
}
