using LvDomain.Common;
using LvDomain.Enums;

namespace LvDomain.Entities.Payroll;

public class PayrollDetailPayment : BaseEntity
{
    public int PayrollDetailId { get; set; }
    public PayrollDetail PayrollDetail { get; set; } = null!;

    public PaymentMethod PaymentMethod { get; set; }
    public decimal Amount { get; set; }
}
