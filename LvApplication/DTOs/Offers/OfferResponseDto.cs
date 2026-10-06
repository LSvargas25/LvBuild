using LvDomain.Enums;

namespace LvApplication.DTOs.Offers;

public class OfferResponseDto
{
    public int Id { get; set; }
    public int BudgetId { get; set; }
    public int CustomerId { get; set; }
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

    /// <summary>Relative URL of the PDF once the offer was sent to the client; null in Draft.</summary>
    public string? PdfUrl { get; set; }
    public int CreatedByUserId { get; set; }

    public List<OfferChapterResponseDto> Chapters { get; set; } = new();
}
