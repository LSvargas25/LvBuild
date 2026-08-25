using LvDomain.Common;
using LvDomain.Entities.Auth;
using LvDomain.Entities.Branches;
using LvDomain.Entities.Budgets;
using LvDomain.Entities.Customers;
using LvDomain.Entities.Offers;
using LvDomain.Enums;

namespace LvDomain.Entities.Projects;

public class Project : BaseEntity
{
    public int OfferId { get; set; }
    public Offer Offer { get; set; } = null!;

    public int BudgetId { get; set; }
    public Budget Budget { get; set; } = null!;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public ProjectType ProjectType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public int WeeksCounter { get; set; }
    public decimal TotalWorkedHours { get; set; }
    public int WorkersUsedCount { get; set; }
    public int MaterialsUsedCount { get; set; }
    public decimal CurrentDirectExpenses { get; set; }
    public decimal PendingExpenses { get; set; }
    public decimal CurrentProfit { get; set; }

    public ProjectStatus Status { get; set; }

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public ICollection<ProjectEndDateHistory> EndDateHistory { get; set; } =
        new List<ProjectEndDateHistory>();
    public ICollection<ProjectWorker> Workers { get; set; } = new List<ProjectWorker>();
}
