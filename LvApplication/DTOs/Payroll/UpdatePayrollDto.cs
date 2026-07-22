namespace LvApplication.DTOs.Payroll;

public class UpdatePayrollDto
{
    public int? ChapterId { get; set; }
    public List<PayrollDetailDto> Details { get; set; } = new();
}
