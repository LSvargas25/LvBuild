using LvApplication.Common;
using LvApplication.DTOs.Commercial;

namespace LvApplication.Services.Commercial;

public interface IInvoiceService
{
    Task<InvoiceDto> CreateAsync(CreateInvoiceDto request, int createdByUserId);
    Task<InvoiceDto> UpdateDraftAsync(int id, UpdateInvoiceDraftDto request);
    Task<InvoiceDto> IssueAsync(int id, IssueInvoiceDto request, int actingUserId);
    Task<InvoiceDto> AddPaymentAsync(int id, CreateInvoicePaymentDto request, int receivedByUserId);
    Task<InvoiceDto> CancelAsync(int id);
    Task DeleteAsync(int id);
    Task<InvoiceDto> GetByIdAsync(int id);
    Task<PagedResult<InvoiceDto>> GetAllByBranchAsync(int branchId, int pageNumber, int pageSize);
}
