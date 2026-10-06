using LvDomain.Entities.Branches;

namespace LvApplication.Services.Branches;

public interface IBranchRepository
{
    Task<Branch?> GetByIdAsync(int id);
    Task<(List<Branch> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        int? operationsDirectorId
    );
    Task<List<Branch>> GetActiveAsync();
    Task AddAsync(Branch branch);
    Task UpdateAsync(Branch branch);
    Task DeleteAsync(Branch branch);
}
