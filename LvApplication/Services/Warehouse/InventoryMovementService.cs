using FluentValidation;
using LvApplication.Common;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Warehouse;
using LvApplication.Services.Branches;
using LvApplication.Services.Commercial;
using LvApplication.Services.Inventory;
using LvApplication.Services.Projects;
using LvDomain.Entities.Warehouse;
using LvDomain.Enums;

namespace LvApplication.Services.Warehouse;

public class InventoryMovementService : IInventoryMovementService
{
    private const string GeneralManagerRole = "GeneralManager";
    private const string OperationsDirectorRole = "OperationsDirector";

    private static bool CanValidate(IEnumerable<string> actingUserRoles) =>
        actingUserRoles.Contains(GeneralManagerRole)
        || actingUserRoles.Contains(OperationsDirectorRole);

    private readonly IInventoryMovementRepository _movementRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProductRepository _productRepository;
    private readonly IBranchInventoryRepository _branchInventoryRepository;
    private readonly IProjectInventoryItemRepository _projectInventoryItemRepository;
    private readonly IValidator<CreateInventoryMovementDto> _createValidator;

    public InventoryMovementService(
        IInventoryMovementRepository movementRepository,
        IBranchRepository branchRepository,
        IProjectRepository projectRepository,
        IProductRepository productRepository,
        IBranchInventoryRepository branchInventoryRepository,
        IProjectInventoryItemRepository projectInventoryItemRepository,
        IValidator<CreateInventoryMovementDto> createValidator
    )
    {
        _movementRepository = movementRepository;
        _branchRepository = branchRepository;
        _projectRepository = projectRepository;
        _productRepository = productRepository;
        _branchInventoryRepository = branchInventoryRepository;
        _projectInventoryItemRepository = projectInventoryItemRepository;
        _createValidator = createValidator;
    }

    public async Task<InventoryMovementDto> CreateAsync(
        CreateInventoryMovementDto request,
        int sentByUserId
    )
    {
        await _createValidator.ValidateAndThrowAppExceptionAsync(request);

        var originBranch =
            await _branchRepository.GetByIdAsync(request.OriginBranchId)
            ?? throw new NotFoundException($"No se encontró la sucursal {request.OriginBranchId}.");

        if (originBranch.BranchType != BranchType.Warehouse)
        {
            throw new ValidationAppException(
                "La sucursal de origen de un movimiento de inventario debe ser de tipo Bodega."
            );
        }

        if (request.DestinationBranchId.HasValue)
        {
            var destinationBranch =
                await _branchRepository.GetByIdAsync(request.DestinationBranchId.Value)
                ?? throw new NotFoundException(
                    $"No se encontró la sucursal {request.DestinationBranchId}."
                );

            if (destinationBranch.BranchType != BranchType.Commercial)
            {
                throw new ValidationAppException(
                    "La sucursal de destino de un movimiento de inventario debe ser de tipo Comercio."
                );
            }
        }
        else
        {
            _ =
                await _projectRepository.GetByIdAsync(request.DestinationProjectId!.Value)
                ?? throw new NotFoundException(
                    $"No se encontró el proyecto {request.DestinationProjectId}."
                );
        }

        _ =
            await _productRepository.GetByIdAsync(request.ProductId)
            ?? throw new NotFoundException($"No se encontró el producto {request.ProductId}.");

        var now = DateTime.UtcNow;
        var movement = new InventoryMovement
        {
            OriginBranchId = request.OriginBranchId,
            DestinationBranchId = request.DestinationBranchId,
            DestinationProjectId = request.DestinationProjectId,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            Status = InventoryMovementStatus.Sent,
            SentByUserId = sentByUserId,
            SentDate = now,
            CreatedAt = now,
        };

        await _movementRepository.AddAsync(movement);

        return MapToDto(movement);
    }

    public async Task<InventoryMovementDto> ValidateAsync(
        int id,
        bool approve,
        int actingUserId,
        IEnumerable<string> actingUserRoles
    )
    {
        if (!CanValidate(actingUserRoles))
        {
            throw new ForbiddenException(
                "Solo Gerente General o Director de Operaciones pueden validar movimientos de inventario."
            );
        }

        var movement =
            await _movementRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"No se encontró el movimiento de inventario {id}.");

        if (movement.Status != InventoryMovementStatus.Sent)
        {
            throw new ValidationAppException("Solo se puede validar un movimiento en estado Sent.");
        }

        if (approve)
        {
            var originInventory = await _branchInventoryRepository.GetByBranchAndProductAsync(
                movement.OriginBranchId,
                movement.ProductId
            );
            if (originInventory is null || originInventory.Quantity < movement.Quantity)
            {
                throw new ValidationAppException(
                    "Stock insuficiente en la bodega de origen para completar el traslado."
                );
            }

            originInventory.Quantity -= movement.Quantity;
            originInventory.UpdatedAt = DateTime.UtcNow;
            await _branchInventoryRepository.UpdateAsync(originInventory);

            if (movement.DestinationBranchId.HasValue)
            {
                await IncrementBranchInventoryAsync(
                    movement.DestinationBranchId.Value,
                    movement.ProductId,
                    movement.Quantity
                );
            }
            else
            {
                var product =
                    await _productRepository.GetByIdAsync(movement.ProductId)
                    ?? throw new NotFoundException(
                        $"No se encontró el producto {movement.ProductId}."
                    );
                await IncrementProjectInventoryAsync(
                    movement.DestinationProjectId!.Value,
                    movement.ProductId,
                    movement.Quantity,
                    product.UnitCost
                );
            }

            movement.Status = InventoryMovementStatus.Accepted;
        }
        else
        {
            movement.Status = InventoryMovementStatus.Denied;
        }

        movement.ValidatedByUserId = actingUserId;
        movement.ValidatedDate = DateTime.UtcNow;
        movement.UpdatedAt = DateTime.UtcNow;

        await _movementRepository.UpdateAsync(movement);

        return MapToDto(movement);
    }

    private async Task IncrementBranchInventoryAsync(int branchId, int productId, decimal quantity)
    {
        var inventory = await _branchInventoryRepository.GetByBranchAndProductAsync(
            branchId,
            productId
        );

        if (inventory is null)
        {
            inventory = new LvDomain.Entities.Commercial.BranchInventory
            {
                BranchId = branchId,
                ProductId = productId,
                Quantity = quantity,
                MinimumStock = 0,
                CreatedAt = DateTime.UtcNow,
            };
            await _branchInventoryRepository.AddAsync(inventory);
        }
        else
        {
            inventory.Quantity += quantity;
            inventory.UpdatedAt = DateTime.UtcNow;
            await _branchInventoryRepository.UpdateAsync(inventory);
        }
    }

    private async Task IncrementProjectInventoryAsync(
        int projectId,
        int productId,
        decimal quantity,
        decimal unitCost
    )
    {
        var item = await _projectInventoryItemRepository.GetByProjectAndProductAsync(
            projectId,
            productId
        );

        if (item is null)
        {
            item = new LvDomain.Entities.Inventory.ProjectInventoryItem
            {
                ProjectId = projectId,
                ProductId = productId,
                CurrentQuantity = quantity,
                ReferenceUnitCost = unitCost,
                CreatedAt = DateTime.UtcNow,
            };
            await _projectInventoryItemRepository.AddAsync(item);
        }
        else
        {
            item.CurrentQuantity += quantity;
            item.ReferenceUnitCost = unitCost;
            item.UpdatedAt = DateTime.UtcNow;
            await _projectInventoryItemRepository.UpdateAsync(item);
        }
    }

    public async Task DeleteAsync(int id, IEnumerable<string> actingUserRoles)
    {
        if (!CanValidate(actingUserRoles))
        {
            throw new ForbiddenException(
                "Solo Gerente General o Director de Operaciones pueden eliminar movimientos de inventario."
            );
        }

        var movement =
            await _movementRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"No se encontró el movimiento de inventario {id}.");

        if (movement.Status != InventoryMovementStatus.Sent)
        {
            throw new ValidationAppException(
                "Solo se puede eliminar un movimiento en estado Sent."
            );
        }

        await _movementRepository.DeleteAsync(movement);
    }

    public async Task<InventoryMovementDto> GetByIdAsync(int id)
    {
        var movement =
            await _movementRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"No se encontró el movimiento de inventario {id}.");
        return MapToDto(movement);
    }

    public async Task<PagedResult<InventoryMovementDto>> GetAllByOriginBranchAsync(
        int branchId,
        int pageNumber,
        int pageSize
    )
    {
        var (items, totalCount) = await _movementRepository.GetPagedByOriginBranchAsync(
            branchId,
            pageNumber,
            pageSize
        );

        return new PagedResult<InventoryMovementDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
    }

    private static InventoryMovementDto MapToDto(InventoryMovement movement) =>
        new()
        {
            Id = movement.Id,
            OriginBranchId = movement.OriginBranchId,
            DestinationBranchId = movement.DestinationBranchId,
            DestinationProjectId = movement.DestinationProjectId,
            ProductId = movement.ProductId,
            Quantity = movement.Quantity,
            Status = movement.Status,
            SentByUserId = movement.SentByUserId,
            SentDate = movement.SentDate,
            ValidatedByUserId = movement.ValidatedByUserId,
            ValidatedDate = movement.ValidatedDate,
        };
}
