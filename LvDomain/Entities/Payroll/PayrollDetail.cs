using LvDomain.Common;
using LvDomain.Entities.Workers;
using LvDomain.Enums;

namespace LvDomain.Entities.Payroll;

public class PayrollDetail : BaseEntity
{
    public int PayrollId { get; set; }
    public Payroll Payroll { get; set; } = null!;

    public int WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    public DateTime Date { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal HourlyRate { get; set; }
    public PayrollPaymentType PaymentType { get; set; }
    public decimal? AdvanceAmountApplied { get; set; }
    public decimal FinalAmountToPay { get; set; }

    public ICollection<PayrollDetailPayment> Payments { get; set; } =
        new List<PayrollDetailPayment>();
}
