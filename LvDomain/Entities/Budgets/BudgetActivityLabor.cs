using LvDomain.Common;
using LvDomain.Enums;

namespace LvDomain.Entities.Budgets;

public class BudgetActivityLabor : BaseEntity
{
    public int ActivityId { get; set; }
    public BudgetActivity Activity { get; set; } = null!;

    public WorkerType WorkerType { get; set; }
    public decimal HourlyRate { get; set; }
}
