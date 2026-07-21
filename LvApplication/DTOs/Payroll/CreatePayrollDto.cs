namespace LvApplication.DTOs.Payroll;

public class CreatePayrollDto
{
    public int SiteLogId { get; set; }
    public List<PayrollDetailDto> Details { get; set; } = new();
}
