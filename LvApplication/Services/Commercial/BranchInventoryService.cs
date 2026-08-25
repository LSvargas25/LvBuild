using LvApplication.DTOs.Commercial;

namespace LvApplication.Services.Commercial;

public class BranchInventoryService : IBranchInventoryService
{
    private readonly IBranchInventoryRepository _repository;

    public BranchInventoryService(IBranchInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<BranchInventoryDto>> GetByBranchAsync(int branchId)
    {
        var items = await _repository.GetByBranchAsync(branchId);

        return items
            .Select(i => new BranchInventoryDto
            {
                Id = i.Id,
                BranchId = i.BranchId,
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                Sku = i.Product.Sku,
                Quantity = i.Quantity,
                MinimumStock = i.MinimumStock,
            })
            .ToList();
    }
}
