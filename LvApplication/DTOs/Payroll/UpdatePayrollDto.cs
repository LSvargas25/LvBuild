namespace LvApplication.DTOs.Payroll;

public class UpdatePayrollDto
{
    public List<PayrollDetailDto> Details { get; set; } = new();
}
