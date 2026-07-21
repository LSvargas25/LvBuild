using LvDomain.Entities.Offers;
using LvDomain.Enums;

namespace LvApplication.Services.Offers;

public interface IOfferRepository
{
    Task<Offer?> GetByIdAsync(int id);
    Task<Offer?> GetByBudgetIdAsync(int budgetId);
    Task<(List<Offer> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, OfferStatus? status);
    Task<int> CountByOfferNumberPrefixAsync(string prefix);
    Task AddAsync(Offer offer);
    Task UpdateAsync(Offer offer);
    Task DeleteAsync(Offer offer);
}
