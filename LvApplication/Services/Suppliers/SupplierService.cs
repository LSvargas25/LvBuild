using FluentValidation;
using LvApplication.Common;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Suppliers;
using LvDomain.Entities.Suppliers;
using LvDomain.Enums;

namespace LvApplication.Services.Suppliers;

public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly IValidator<CreateSupplierDto> _createValidator;
    private readonly IValidator<UpdateSupplierDto> _updateValidator;

    public SupplierService(
        ISupplierRepository supplierRepository,
        IValidator<CreateSupplierDto> createValidator,
        IValidator<UpdateSupplierDto> updateValidator
    )
    {
        _supplierRepository = supplierRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<SupplierResponseDto> CreateAsync(CreateSupplierDto request)
    {
        await _createValidator.ValidateAndThrowAppExceptionAsync(request);

        var supplier = new Supplier
        {
            Name = request.Name,
            Status = ActiveStatus.Active,
            City = request.City,
            PhoneNumber = request.PhoneNumber,
            PersonalId = request.PersonalId,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow,
        };

        await _supplierRepository.AddAsync(supplier);

        return MapToDto(supplier);
    }

    public async Task<SupplierResponseDto> UpdateAsync(int id, UpdateSupplierDto request)
    {
        await _updateValidator.ValidateAndThrowAppExceptionAsync(request);

        var supplier =
            await _supplierRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Supplier {id} not found.");

        supplier.Name = request.Name;
        supplier.Status = request.Status;
        supplier.City = request.City;
        supplier.PhoneNumber = request.PhoneNumber;
        supplier.PersonalId = request.PersonalId;
        supplier.Email = request.Email;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _supplierRepository.UpdateAsync(supplier);

        return MapToDto(supplier);
    }

    public async Task<SupplierResponseDto> GetByIdAsync(int id)
    {
        var supplier =
            await _supplierRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Supplier {id} not found.");
        return MapToDto(supplier);
    }

    public async Task<PagedResult<SupplierResponseDto>> GetAllAsync(int pageNumber, int pageSize)
    {
        var (items, totalCount) = await _supplierRepository.GetPagedAsync(pageNumber, pageSize);

        return new PagedResult<SupplierResponseDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
    }

    public async Task DeleteAsync(int id)
    {
        var supplier =
            await _supplierRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Supplier {id} not found.");

        supplier.Status = ActiveStatus.Inactive;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _supplierRepository.UpdateAsync(supplier);
    }

    private static SupplierResponseDto MapToDto(Supplier supplier) =>
        new()
        {
            Id = supplier.Id,
            Name = supplier.Name,
            Status = supplier.Status,
            City = supplier.City,
            PhoneNumber = supplier.PhoneNumber,
            PersonalId = supplier.PersonalId,
            Email = supplier.Email,
        };
}
