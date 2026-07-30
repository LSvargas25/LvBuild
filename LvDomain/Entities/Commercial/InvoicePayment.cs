using LvDomain.Common;
using LvDomain.Entities.Auth;
using LvDomain.Enums;

namespace LvDomain.Entities.Commercial;

public class InvoicePayment : BaseEntity
{
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public InvoicePaymentMethod PaymentMethod { get; set; }

    public int ReceivedByUserId { get; set; }
    public User ReceivedByUser { get; set; } = null!;
}
