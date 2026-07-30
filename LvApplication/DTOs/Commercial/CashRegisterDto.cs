using LvDomain.Enums;

namespace LvApplication.DTOs.Commercial;

public class CashRegisterDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int OpenedByUserId { get; set; }
    public DateTime OpeningDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public CashRegisterStatus Status { get; set; }
    public int? ClosedByUserId { get; set; }
    public DateTime? ClosingDate { get; set; }
    public decimal? ClosingBalance { get; set; }
    public decimal? ExpectedBalance { get; set; }
    public decimal? Difference { get; set; }
}
