using LvDomain.Entities.Customers;

namespace LvApplication.Services.Customers;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int id);
    Task<(List<Customer> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);
    Task AddAsync(Customer customer);
    Task UpdateAsync(Customer customer);
    Task DeleteAsync(Customer customer);
}
