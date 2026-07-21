using LvDomain.Enums;

namespace LvApplication.DTOs.Suppliers;

public class UpdateSupplierDto
{
    public string Name { get; set; } = string.Empty;
    public ActiveStatus Status { get; set; }
    public string? City { get; set; }
    public string? PhoneNumber { get; set; }
    public string? PersonalId { get; set; }
    public string? Email { get; set; }
}
