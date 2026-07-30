using LvDomain.Enums;

namespace LvApplication.DTOs.Commercial;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? UnitOfMeasure { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UnitCost { get; set; }
    public string? Category { get; set; }
    public ProductStatus Status { get; set; }
    public bool ActiveStatus { get; set; }
    public int CreatedByUserId { get; set; }
    public int? ValidatedByUserId { get; set; }
    public DateTime? ValidatedDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
