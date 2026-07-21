using LvDomain.Enums;

namespace LvApplication.DTOs.Customers;

public class CustomerResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CustomerType CustomerType { get; set; }
    public ActiveStatus Status { get; set; }
    public string? City { get; set; }
    public string? PhoneNumber { get; set; }
    public string? PersonalId { get; set; }
    public string? Email { get; set; }
    public int? BranchId { get; set; }
}
