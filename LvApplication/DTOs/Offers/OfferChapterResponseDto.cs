namespace LvApplication.DTOs.Offers;

public class OfferChapterResponseDto
{
    public int Id { get; set; }
    public string ChapterName { get; set; } = string.Empty;
    public int EstimatedWeeks { get; set; }
    public decimal? ApproxMaterialQuantity { get; set; }
}
