using LvDomain.Enums;

namespace LvApplication.DTOs.Commercial;

public class InvoicePaymentDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public InvoicePaymentMethod PaymentMethod { get; set; }
    public int ReceivedByUserId { get; set; }
}
