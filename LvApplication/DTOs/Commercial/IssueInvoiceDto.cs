namespace LvApplication.DTOs.Commercial;

public class IssueInvoiceDto
{
    public List<CreateInvoicePaymentDto> Payments { get; set; } = new();
}
