using FluentValidation;
using LvApplication.Common;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Commercial;
using LvDomain.Entities.Commercial;
using LvDomain.Enums;

namespace LvApplication.Services.Commercial;

public class ProductService : IProductService
{
    private const string GeneralManagerRole = "GeneralManager";
    private const string OperationsDirectorRole = "OperationsDirector";
    private const string BranchAdminRole = "BranchAdmin";

    private readonly IProductRepository _productRepository;
    private readonly IValidator<CreateProductDto> _createValidator;
    private readonly IValidator<UpdateProductDto> _updateValidator;

    public ProductService(
        IProductRepository productRepository,
        IValidator<CreateProductDto> createValidator,
        IValidator<UpdateProductDto> updateValidator
    )
    {
        _productRepository = productRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    private static bool CanAutoValidate(IEnumerable<string> actingUserRoles) =>
        actingUserRoles.Contains(GeneralManagerRole)
        || actingUserRoles.Contains(OperationsDirectorRole)
        || actingUserRoles.Contains(BranchAdminRole);

    public async Task<ProductDto> CreateAsync(
        CreateProductDto request,
        int createdByUserId,
        IEnumerable<string> actingUserRoles
    )
    {
        await _createValidator.ValidateAndThrowAppExceptionAsync(request);

        var existing = await _productRepository.GetBySkuAsync(request.Sku);
        if (existing is not null)
        {
            throw new ConflictException($"Ya existe un producto con el código {request.Sku}.");
        }

        var autoValidate = CanAutoValidate(actingUserRoles);
        var now = DateTime.UtcNow;

        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Sku = request.Sku,
            UnitOfMeasure = request.UnitOfMeasure,
            UnitPrice = request.UnitPrice,
            UnitCost = request.UnitCost,
            Category = request.Category,
            CreatedByUserId = createdByUserId,
            Status = autoValidate ? ProductStatus.Validated : ProductStatus.PendingValidation,
            ActiveStatus = true,
            ValidatedByUserId = autoValidate ? createdByUserId : null,
            ValidatedDate = autoValidate ? now : null,
            CreatedAt = now,
        };

        await _productRepository.AddAsync(product);

        return MapToDto(product);
    }

    public async Task<ProductDto> UpdateAsync(
        int id,
        UpdateProductDto request,
        IEnumerable<string> actingUserRoles
    )
    {
        await _updateValidator.ValidateAndThrowAppExceptionAsync(request);

        var product =
            await _productRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Product {id} not found.");

        var autoValidate = CanAutoValidate(actingUserRoles);
        var now = DateTime.UtcNow;

        product.Name = request.Name;
        product.Description = request.Description;
        product.UnitOfMeasure = request.UnitOfMeasure;
        product.UnitPrice = request.UnitPrice;
        product.UnitCost = request.UnitCost;
        product.Category = request.Category;
        product.UpdatedAt = now;

        if (autoValidate)
        {
            product.Status = ProductStatus.Validated;
            product.ValidatedByUserId = null;
            product.ValidatedDate = now;
        }
        else
        {
            product.Status = ProductStatus.PendingValidation;
            product.ValidatedByUserId = null;
            product.ValidatedDate = null;
        }

        await _productRepository.UpdateAsync(product);

        return MapToDto(product);
    }

    public async Task<ProductDto> ValidateAsync(
        int id,
        bool approve,
        int actingUserId,
        IEnumerable<string> actingUserRoles
    )
    {
        if (!CanAutoValidate(actingUserRoles))
        {
            throw new ForbiddenException(
                "Solo Gerente General, Director de Operaciones o Administrador de Sucursal pueden validar productos."
            );
        }

        var product =
            await _productRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Product {id} not found.");

        if (product.Status != ProductStatus.PendingValidation)
        {
            throw new ValidationAppException(
                "Solo se puede validar un producto en estado PendingValidation."
            );
        }

        product.Status = approve ? ProductStatus.Validated : ProductStatus.Rejected;
        product.ValidatedByUserId = actingUserId;
        product.ValidatedDate = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.UpdateAsync(product);

        return MapToDto(product);
    }

    public async Task<ProductDto> DeactivateAsync(int id)
    {
        var product =
            await _productRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Product {id} not found.");
        product.ActiveStatus = false;
        product.UpdatedAt = DateTime.UtcNow;
        await _productRepository.UpdateAsync(product);
        return MapToDto(product);
    }

    public async Task<ProductDto> ActivateAsync(int id)
    {
        var product =
            await _productRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Product {id} not found.");
        product.ActiveStatus = true;
        product.UpdatedAt = DateTime.UtcNow;
        await _productRepository.UpdateAsync(product);
        return MapToDto(product);
    }

    public async Task<ProductDto> GetByIdAsync(int id)
    {
        var product =
            await _productRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Product {id} not found.");
        return MapToDto(product);
    }

    public async Task<PagedResult<ProductDto>> GetAllAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly
    )
    {
        var (items, totalCount) = await _productRepository.GetPagedAsync(
            pageNumber,
            pageSize,
            activeOnly
        );

        return new PagedResult<ProductDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
    }

    private static ProductDto MapToDto(Product product) =>
        new()
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Sku = product.Sku,
            UnitOfMeasure = product.UnitOfMeasure,
            UnitPrice = product.UnitPrice,
            UnitCost = product.UnitCost,
            Category = product.Category,
            Status = product.Status,
            ActiveStatus = product.ActiveStatus,
            CreatedByUserId = product.CreatedByUserId,
            ValidatedByUserId = product.ValidatedByUserId,
            ValidatedDate = product.ValidatedDate,
            CreatedAt = product.CreatedAt,
        };
}
