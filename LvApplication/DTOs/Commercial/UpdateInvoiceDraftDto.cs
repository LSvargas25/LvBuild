using LvDomain.Enums;

namespace LvApplication.DTOs.Commercial;

public class UpdateInvoiceDraftDto
{
    public int? CustomerId { get; set; }
    public InvoicePaymentType PaymentType { get; set; }
    public List<InvoiceDetailLineDto> Details { get; set; } = new();
}
