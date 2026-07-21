using LvDomain.Enums;

namespace LvApplication.DTOs.Payroll;

public class PayrollDetailDto
{
    public int WorkerId { get; set; }
    public DateTime Date { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal HourlyRate { get; set; }
    public PayrollPaymentType PaymentType { get; set; }
    public decimal? AdvanceAmountApplied { get; set; }
    public List<PayrollDetailPaymentDto> Payments { get; set; } = new();
}
