using LvDomain.Enums;

namespace LvApplication.DTOs.Payroll;

public class PayrollDetailPaymentDto
{
    public PaymentMethod PaymentMethod { get; set; }
    public decimal Amount { get; set; }
}
