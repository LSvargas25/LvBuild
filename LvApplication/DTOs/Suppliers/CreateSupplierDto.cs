namespace LvApplication.DTOs.Suppliers;

public class CreateSupplierDto
{
    public string Name { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? PhoneNumber { get; set; }
    public string? PersonalId { get; set; }
    public string? Email { get; set; }
}
