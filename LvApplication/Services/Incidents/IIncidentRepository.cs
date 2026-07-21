using LvDomain.Entities.Incidents;

namespace LvApplication.Services.Incidents;

public interface IIncidentRepository
{
    Task<Incident?> GetByIdAsync(int id);
    Task<(List<Incident> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);
    Task<(List<Incident> Items, int TotalCount)> GetPagedByProjectAsync(int projectId, int pageNumber, int pageSize);
    Task AddAsync(Incident incident);
    Task UpdateAsync(Incident incident);
    Task DeleteAsync(Incident incident);
}
