using LvDomain.Common;
using LvDomain.Entities.Auth;
using LvDomain.Entities.Branches;
using LvDomain.Entities.Customers;
using LvDomain.Enums;

namespace LvDomain.Entities.Commercial;

public class Invoice : BaseEntity
{
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public int CashRegisterId { get; set; }
    public CashRegister CashRegister { get; set; } = null!;

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string? InvoiceNumber { get; set; }
    public DateTime Date { get; set; }
    public InvoicePaymentType PaymentType { get; set; }
    public InvoiceStatus Status { get; set; }

    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public ICollection<InvoiceDetail> Details { get; set; } = new List<InvoiceDetail>();
    public ICollection<InvoicePayment> Payments { get; set; } = new List<InvoicePayment>();
}
