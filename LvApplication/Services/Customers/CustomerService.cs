using FluentValidation;
using LvApplication.Common;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Customers;
using LvDomain.Entities.Customers;
using LvDomain.Enums;

namespace LvApplication.Services.Customers;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IValidator<CreateCustomerDto> _createValidator;
    private readonly IValidator<UpdateCustomerDto> _updateValidator;

    public CustomerService(
        ICustomerRepository customerRepository,
        IValidator<CreateCustomerDto> createValidator,
        IValidator<UpdateCustomerDto> updateValidator
    )
    {
        _customerRepository = customerRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<CustomerResponseDto> CreateAsync(CreateCustomerDto request)
    {
        await _createValidator.ValidateAndThrowAppExceptionAsync(request);

        var customer = new Customer
        {
            Name = request.Name,
            CustomerType = request.CustomerType,
            Status = ActiveStatus.Active,
            City = request.City,
            PhoneNumber = request.PhoneNumber,
            PersonalId = request.PersonalId,
            Email = request.Email,
            BranchId = request.BranchId,
            CreatedAt = DateTime.UtcNow,
        };

        await _customerRepository.AddAsync(customer);

        return MapToDto(customer);
    }

    public async Task<CustomerResponseDto> UpdateAsync(int id, UpdateCustomerDto request)
    {
        await _updateValidator.ValidateAndThrowAppExceptionAsync(request);

        var customer =
            await _customerRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Customer {id} not found.");

        customer.Name = request.Name;
        customer.CustomerType = request.CustomerType;
        customer.Status = request.Status;
        customer.City = request.City;
        customer.PhoneNumber = request.PhoneNumber;
        customer.PersonalId = request.PersonalId;
        customer.Email = request.Email;
        customer.BranchId = request.BranchId;
        customer.UpdatedAt = DateTime.UtcNow;

        await _customerRepository.UpdateAsync(customer);

        return MapToDto(customer);
    }

    public async Task<CustomerResponseDto> GetByIdAsync(int id)
    {
        var customer =
            await _customerRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Customer {id} not found.");
        return MapToDto(customer);
    }

    public async Task<PagedResult<CustomerResponseDto>> GetAllAsync(int pageNumber, int pageSize)
    {
        var (items, totalCount) = await _customerRepository.GetPagedAsync(pageNumber, pageSize);

        return new PagedResult<CustomerResponseDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
    }

    public async Task DeleteAsync(int id)
    {
        var customer =
            await _customerRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Customer {id} not found.");

        customer.Status = ActiveStatus.Inactive;
        customer.UpdatedAt = DateTime.UtcNow;

        await _customerRepository.UpdateAsync(customer);
    }

    private static CustomerResponseDto MapToDto(Customer customer) =>
        new()
        {
            Id = customer.Id,
            Name = customer.Name,
            CustomerType = customer.CustomerType,
            Status = customer.Status,
            City = customer.City,
            PhoneNumber = customer.PhoneNumber,
            PersonalId = customer.PersonalId,
            Email = customer.Email,
            BranchId = customer.BranchId,
        };
}
