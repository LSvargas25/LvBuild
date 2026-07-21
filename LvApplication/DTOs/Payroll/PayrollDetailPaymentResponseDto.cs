using LvDomain.Enums;

namespace LvApplication.DTOs.Payroll;

public class PayrollDetailPaymentResponseDto
{
    public int Id { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal Amount { get; set; }
}
