using LvDomain.Common;

namespace LvDomain.Entities.Offers;

public class OfferChapter : BaseEntity
{
    public int OfferId { get; set; }
    public Offer Offer { get; set; } = null!;

    public string ChapterName { get; set; } = string.Empty;
    public int EstimatedWeeks { get; set; }
    public decimal? ApproxMaterialQuantity { get; set; }
}
