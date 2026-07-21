using LvApplication.Common;
using LvApplication.DTOs.Inventory;

namespace LvApplication.Services.Inventory;

public interface IMaterialTicketService
{
    Task<MaterialTicketDto> CreateAsync(int projectId, CreateMaterialTicketDto request, int createdByUserId);
    Task<MaterialTicketDto> UpdateAsync(int id, UpdateMaterialTicketDto request);
    Task<MaterialTicketDto> ApplyAsync(int id);
    Task<MaterialTicketDto> ArchiveAsync(int id);
    Task DeleteAsync(int id);
    Task<MaterialTicketDto> GetByIdAsync(int id);
    Task<PagedResult<MaterialTicketDto>> GetAllByProjectAsync(int projectId, int pageNumber, int pageSize);
    Task<List<ProjectInventoryItemDto>> GetInventoryAsync(int projectId);
}
