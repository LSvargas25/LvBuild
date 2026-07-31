using LvDomain.Common;
using LvDomain.Entities.Auth;
using LvDomain.Entities.Branches;
using LvDomain.Entities.Commercial;
using LvDomain.Entities.Projects;
using LvDomain.Enums;

namespace LvDomain.Entities.Warehouse;

public class InventoryMovement : BaseEntity
{
    public int OriginBranchId { get; set; }
    public Branch OriginBranch { get; set; } = null!;

    public int? DestinationBranchId { get; set; }
    public Branch? DestinationBranch { get; set; }

    public int? DestinationProjectId { get; set; }
    public Project? DestinationProject { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal Quantity { get; set; }

    public InventoryMovementStatus Status { get; set; }

    public int SentByUserId { get; set; }
    public User SentByUser { get; set; } = null!;
    public DateTime SentDate { get; set; }

    public int? ValidatedByUserId { get; set; }
    public User? ValidatedByUser { get; set; }
    public DateTime? ValidatedDate { get; set; }
}
