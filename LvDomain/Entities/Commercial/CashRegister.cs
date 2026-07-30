using LvDomain.Common;
using LvDomain.Entities.Auth;
using LvDomain.Entities.Branches;
using LvDomain.Enums;

namespace LvDomain.Entities.Commercial;

public class CashRegister : BaseEntity
{
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public int OpenedByUserId { get; set; }
    public User OpenedByUser { get; set; } = null!;
    public DateTime OpeningDate { get; set; }
    public decimal OpeningBalance { get; set; }

    public CashRegisterStatus Status { get; set; }

    public int? ClosedByUserId { get; set; }
    public User? ClosedByUser { get; set; }
    public DateTime? ClosingDate { get; set; }
    public decimal? ClosingBalance { get; set; }
    public decimal? ExpectedBalance { get; set; }
    public decimal? Difference { get; set; }
}
