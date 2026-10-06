using LvDomain.Common;
using LvDomain.Entities.Auth;
using LvDomain.Entities.Budgets;
using LvDomain.Entities.Customers;
using LvDomain.Enums;

namespace LvDomain.Entities.Offers;

public class Offer : BaseEntity
{
    public int BudgetId { get; set; }
    public Budget Budget { get; set; } = null!;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public string OfferNumber { get; set; } = string.Empty;
    public OfferType OfferType { get; set; }
    public DateTime IssueDate { get; set; }
    public int ValidityDays { get; set; }
    public string WorkLocation { get; set; } = string.Empty;
    public string WorkScope { get; set; } = string.Empty;
    public DateTime EstimatedStartDate { get; set; }
    public int EstimatedDurationWeeks { get; set; }
    public DateTime EstimatedDeliveryDate { get; set; }
    public string PaymentTerms { get; set; } = string.Empty;
    public string Warranties { get; set; } = string.Empty;
    public string Exclusions { get; set; } = string.Empty;

    public decimal? TotalProjectPrice { get; set; }
    public decimal? AgreedPercentage { get; set; }
    public string? PercentageIncludes { get; set; }
    public string? PercentageExcludes { get; set; }
    public string? PercentageCalculationMethod { get; set; }
    public PaymentFrequency? PaymentFrequency { get; set; }

    public OfferStatus Status { get; set; }

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public ICollection<OfferChapter> Chapters { get; set; } = new List<OfferChapter>();
}
