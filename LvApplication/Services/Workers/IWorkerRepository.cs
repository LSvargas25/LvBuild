using LvDomain.Entities.Workers;

namespace LvApplication.Services.Workers;

public interface IWorkerRepository
{
    Task<Worker?> GetByIdAsync(int id);
    Task<(List<Worker> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);
    Task AddAsync(Worker worker);
    Task UpdateAsync(Worker worker);
    Task DeleteAsync(Worker worker);
}
