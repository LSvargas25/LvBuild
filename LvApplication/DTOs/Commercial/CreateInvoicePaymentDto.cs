using LvDomain.Enums;

namespace LvApplication.DTOs.Commercial;

public class CreateInvoicePaymentDto
{
    public InvoicePaymentMethod PaymentMethod { get; set; }
    public decimal Amount { get; set; }
}
