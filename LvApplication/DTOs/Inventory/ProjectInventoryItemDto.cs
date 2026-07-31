namespace LvApplication.DTOs.Inventory;

public class ProjectInventoryItemDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int? MaterialId { get; set; }
    public int? ProductId { get; set; }
    public decimal CurrentQuantity { get; set; }
    public decimal ReferenceUnitCost { get; set; }
}
