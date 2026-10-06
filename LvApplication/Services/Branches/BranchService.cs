using FluentValidation;
using LvApplication.Common;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Branches;
using LvApplication.Services.Auth;
using LvDomain.Entities.Branches;
using LvDomain.Enums;

namespace LvApplication.Services.Branches;

public class BranchService : IBranchService
{
    private const string GeneralManagerRole = "GeneralManager";
    private const string OperationsDirectorRole = "OperationsDirector";
    private const string BranchAdminRole = "BranchAdmin";
    private const string BusinessManagerRole = "BusinessManager";

    private readonly IBranchRepository _branchRepository;
    private readonly IUserRepository _userRepository;
    private readonly IValidator<CreateBranchDto> _createValidator;
    private readonly IValidator<UpdateBranchDto> _updateValidator;

    public BranchService(
        IBranchRepository branchRepository,
        IUserRepository userRepository,
        IValidator<CreateBranchDto> createValidator,
        IValidator<UpdateBranchDto> updateValidator
    )
    {
        _branchRepository = branchRepository;
        _userRepository = userRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<BranchResponseDto> CreateAsync(CreateBranchDto request)
    {
        await _createValidator.ValidateAndThrowAppExceptionAsync(request);

        await EnsureUserHasRoleAsync(
            request.OperationsDirectorId,
            OperationsDirectorRole,
            "Director de Operaciones"
        );

        if (request.BranchAdminId.HasValue)
        {
            await EnsureUserHasRoleAsync(
                request.BranchAdminId.Value,
                BranchAdminRole,
                "Administrador de Sucursal"
            );
        }

        if (request.BusinessManagerId.HasValue)
        {
            await EnsureUserHasRoleAsync(
                request.BusinessManagerId.Value,
                BusinessManagerRole,
                "Gerente de Negocio"
            );
        }

        var branch = new Branch
        {
            Name = request.Name,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            City = request.City,
            Province = request.Province,
            Status = BranchStatus.Active,
            BranchType = request.BranchType,
            OperationsDirectorId = request.OperationsDirectorId,
            BranchAdminId = request.BranchAdminId,
            BusinessManagerId = request.BusinessManagerId,
            CreatedAt = DateTime.UtcNow,
        };

        branch.Indicator = new BranchIndicator
        {
            Profit = 0,
            Losses = 0,
            DirectExpenses = 0,
            IndirectExpenses = 0,
            TotalWorkers = 0,
            TotalMaterials = 0,
            LastUpdatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };

        await _branchRepository.AddAsync(branch);

        return MapToDto(branch);
    }

    public async Task<BranchResponseDto> UpdateAsync(int id, UpdateBranchDto request)
    {
        await _updateValidator.ValidateAndThrowAppExceptionAsync(request);

        var branch =
            await _branchRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"No se encontró la sucursal {id}.");

        if (request.BranchAdminId.HasValue)
        {
            await EnsureUserHasRoleAsync(
                request.BranchAdminId.Value,
                BranchAdminRole,
                "Administrador de Sucursal"
            );
        }

        if (request.BusinessManagerId.HasValue)
        {
            await EnsureUserHasRoleAsync(
                request.BusinessManagerId.Value,
                BusinessManagerRole,
                "Gerente de Negocio"
            );
        }

        branch.Name = request.Name;
        branch.PhoneNumber = request.PhoneNumber;
        branch.Email = request.Email;
        branch.City = request.City;
        branch.Province = request.Province;
        branch.BranchType = request.BranchType;
        branch.BranchAdminId = request.BranchAdminId;
        branch.BusinessManagerId = request.BusinessManagerId;
        branch.UpdatedAt = DateTime.UtcNow;

        await _branchRepository.UpdateAsync(branch);

        return MapToDto(branch);
    }

    public async Task<BranchResponseDto> AssignOperationsDirectorAsync(
        int id,
        AssignOperationsDirectorDto request
    )
    {
        var branch =
            await _branchRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"No se encontró la sucursal {id}.");

        await EnsureUserHasRoleAsync(
            request.OperationsDirectorId,
            OperationsDirectorRole,
            "Director de Operaciones"
        );

        branch.OperationsDirectorId = request.OperationsDirectorId;
        branch.UpdatedAt = DateTime.UtcNow;

        await _branchRepository.UpdateAsync(branch);

        return MapToDto(branch);
    }

    public async Task<BranchResponseDto> ActivateAsync(int id)
    {
        var branch =
            await _branchRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"No se encontró la sucursal {id}.");

        branch.Status = BranchStatus.Active;
        branch.UpdatedAt = DateTime.UtcNow;

        await _branchRepository.UpdateAsync(branch);

        return MapToDto(branch);
    }

    public async Task<BranchResponseDto> DeactivateAsync(int id)
    {
        var branch =
            await _branchRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"No se encontró la sucursal {id}.");

        branch.Status = BranchStatus.Inactive;
        branch.UpdatedAt = DateTime.UtcNow;

        await _branchRepository.UpdateAsync(branch);

        return MapToDto(branch);
    }

    public async Task DeleteAsync(int id, bool isGeneralManager)
    {
        var branch =
            await _branchRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"No se encontró la sucursal {id}.");

        if (isGeneralManager)
        {
            await _branchRepository.DeleteAsync(branch);
            return;
        }

        branch.Status = BranchStatus.Archived;
        branch.UpdatedAt = DateTime.UtcNow;
        await _branchRepository.UpdateAsync(branch);
    }

    public async Task<BranchResponseDto> GetByIdAsync(
        int id,
        int currentUserId,
        IEnumerable<string> currentUserRoles
    )
    {
        var branch =
            await _branchRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"No se encontró la sucursal {id}.");

        if (!IsGeneralManager(currentUserRoles) && branch.OperationsDirectorId != currentUserId)
        {
            throw new NotFoundException($"No se encontró la sucursal {id}.");
        }

        return MapToDto(branch);
    }

    public async Task<PagedResult<BranchResponseDto>> GetAllAsync(
        int pageNumber,
        int pageSize,
        int currentUserId,
        IEnumerable<string> currentUserRoles
    )
    {
        var operationsDirectorFilter = IsGeneralManager(currentUserRoles)
            ? (int?)null
            : currentUserId;

        var (items, totalCount) = await _branchRepository.GetPagedAsync(
            pageNumber,
            pageSize,
            operationsDirectorFilter
        );

        return new PagedResult<BranchResponseDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
    }

    public async Task<List<BranchOptionDto>> GetOptionsAsync()
    {
        var branches = await _branchRepository.GetActiveAsync();
        return branches
            .Select(b => new BranchOptionDto
            {
                Id = b.Id,
                Name = b.Name,
                BranchType = b.BranchType,
            })
            .ToList();
    }

    private static bool IsGeneralManager(IEnumerable<string> roles) =>
        roles.Contains(GeneralManagerRole);

    private async Task EnsureUserHasRoleAsync(
        int userId,
        string expectedRole,
        string fieldDescription
    )
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user is null || !user.UserRoles.Any(ur => ur.Role.Name == expectedRole))
        {
            throw new ValidationAppException(
                $"El usuario asignado como {fieldDescription} no tiene el rol {expectedRole}."
            );
        }
    }

    private static BranchResponseDto MapToDto(Branch branch) =>
        new()
        {
            Id = branch.Id,
            Name = branch.Name,
            PhoneNumber = branch.PhoneNumber,
            Email = branch.Email,
            City = branch.City,
            Province = branch.Province,
            Status = branch.Status,
            BranchType = branch.BranchType,
            OperationsDirectorId = branch.OperationsDirectorId,
            BranchAdminId = branch.BranchAdminId,
            BusinessManagerId = branch.BusinessManagerId,
            Indicator = branch.Indicator is null
                ? null
                : new BranchIndicatorDto
                {
                    Profit = branch.Indicator.Profit,
                    Losses = branch.Indicator.Losses,
                    DirectExpenses = branch.Indicator.DirectExpenses,
                    IndirectExpenses = branch.Indicator.IndirectExpenses,
                    TotalWorkers = branch.Indicator.TotalWorkers,
                    TotalMaterials = branch.Indicator.TotalMaterials,
                    LastUpdatedAt = branch.Indicator.LastUpdatedAt,
                },
        };
}
