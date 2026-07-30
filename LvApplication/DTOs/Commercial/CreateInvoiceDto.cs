using LvDomain.Enums;

namespace LvApplication.DTOs.Commercial;

public class CreateInvoiceDto
{
    public int BranchId { get; set; }
    public int CashRegisterId { get; set; }
    public int? CustomerId { get; set; }
    public InvoicePaymentType PaymentType { get; set; }
    public List<InvoiceDetailLineDto> Details { get; set; } = new();
}
