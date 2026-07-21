using LvDomain.Enums;

namespace LvApplication.DTOs.Payroll;

public class PayrollDetailResponseDto
{
    public int Id { get; set; }
    public int WorkerId { get; set; }
    public DateTime Date { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal HourlyRate { get; set; }
    public PayrollPaymentType PaymentType { get; set; }
    public decimal? AdvanceAmountApplied { get; set; }
    public decimal FinalAmountToPay { get; set; }
    public List<PayrollDetailPaymentResponseDto> Payments { get; set; } = new();
}
