using LvDomain.Enums;

namespace LvApplication.DTOs.Payroll;

public class PayrollDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int SiteLogId { get; set; }
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public decimal TotalPayroll { get; set; }
    public PayrollStatus Status { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime? PaidAt { get; set; }
    public List<PayrollDetailResponseDto> Details { get; set; } = new();
}
