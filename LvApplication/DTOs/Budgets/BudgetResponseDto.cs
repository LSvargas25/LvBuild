using LvDomain.Enums;

namespace LvApplication.DTOs.Budgets;

public class BudgetResponseDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public BudgetStatus Status { get; set; }

    /// <summary>Offer created from this budget, if any (only filled by GET /budgets/{id}).</summary>
    public int? OfferId { get; set; }

    /// <summary>Project started from that offer, if any (only filled by GET /budgets/{id}).</summary>
    public int? ProjectId { get; set; }
    public decimal UtilityPercentage { get; set; }
    public decimal IndirectCostsTotal { get; set; }
    public decimal TotalBudget { get; set; }
    public int CreatedByUserId { get; set; }
    public List<BudgetChapterResponseDto> Chapters { get; set; } = new();
}
