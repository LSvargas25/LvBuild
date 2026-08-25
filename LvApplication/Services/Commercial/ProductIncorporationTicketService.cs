using FluentValidation;
using LvApplication.Common;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Commercial;
using LvApplication.Services.Branches;
using LvDomain.Entities.Commercial;
using LvDomain.Enums;

namespace LvApplication.Services.Commercial;

public class ProductIncorporationTicketService : IProductIncorporationTicketService
{
    private const string GeneralManagerRole = "GeneralManager";
    private const string OperationsDirectorRole = "OperationsDirector";
    private const string BranchAdminRole = "BranchAdmin";

    private readonly IProductIncorporationTicketRepository _ticketRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IBranchInventoryRepository _inventoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly IValidator<CreateProductIncorporationTicketDto> _createValidator;

    public ProductIncorporationTicketService(
        IProductIncorporationTicketRepository ticketRepository,
        IBranchRepository branchRepository,
        IBranchInventoryRepository inventoryRepository,
        IProductRepository productRepository,
        IValidator<CreateProductIncorporationTicketDto> createValidator
    )
    {
        _ticketRepository = ticketRepository;
        _branchRepository = branchRepository;
        _inventoryRepository = inventoryRepository;
        _productRepository = productRepository;
        _createValidator = createValidator;
    }

    private static bool CanAutoValidate(IEnumerable<string> actingUserRoles) =>
        actingUserRoles.Contains(GeneralManagerRole)
        || actingUserRoles.Contains(OperationsDirectorRole)
        || actingUserRoles.Contains(BranchAdminRole);

    public async Task<ProductIncorporationTicketDto> CreateAsync(
        CreateProductIncorporationTicketDto request,
        int createdByUserId,
        IEnumerable<string> actingUserRoles
    )
    {
        await _createValidator.ValidateAndThrowAppExceptionAsync(request);

        var branch =
            await _branchRepository.GetByIdAsync(request.BranchId)
            ?? throw new NotFoundException($"Branch {request.BranchId} not found.");

        if (branch.BranchType is not (BranchType.Commercial or BranchType.Warehouse))
        {
            throw new ValidationAppException(
                "La sucursal debe ser de tipo Comercio o Bodega para incorporar productos."
            );
        }

        var product =
            await _productRepository.GetByIdAsync(request.ProductId)
            ?? throw new NotFoundException($"Product {request.ProductId} not found.");

        if (product.Status != ProductStatus.Validated)
        {
            throw new ValidationAppException(
                "Solo se pueden incorporar productos en estado Validated."
            );
        }

        var autoValidate = CanAutoValidate(actingUserRoles);
        var now = DateTime.UtcNow;

        var ticket = new ProductIncorporationTicket
        {
            BranchId = request.BranchId,
            ProductId = request.ProductId,
            SupplierId = request.SupplierId,
            Quantity = request.Quantity,
            UnitCost = request.UnitCost,
            CreatedByUserId = createdByUserId,
            CreatedDate = now,
            Status = autoValidate
                ? ProductIncorporationTicketStatus.Validated
                : ProductIncorporationTicketStatus.PendingValidation,
            ValidatedByUserId = autoValidate ? createdByUserId : null,
            ValidatedDate = autoValidate ? now : null,
            CreatedAt = now,
        };

        await _ticketRepository.AddAsync(ticket);

        if (autoValidate)
        {
            await IncrementInventoryAsync(ticket);
        }

        return MapToDto(ticket);
    }

    public async Task<ProductIncorporationTicketDto> ValidateAsync(
        int id,
        bool approve,
        int actingUserId,
        IEnumerable<string> actingUserRoles
    )
    {
        if (!CanAutoValidate(actingUserRoles))
        {
            throw new ForbiddenException(
                "Solo Gerente General, Director de Operaciones o Administrador de Sucursal pueden validar incorporaciones."
            );
        }

        var ticket =
            await _ticketRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"ProductIncorporationTicket {id} not found.");

        if (ticket.Status != ProductIncorporationTicketStatus.PendingValidation)
        {
            throw new ValidationAppException(
                "Solo se puede validar un ticket en estado PendingValidation."
            );
        }

        ticket.Status = approve
            ? ProductIncorporationTicketStatus.Validated
            : ProductIncorporationTicketStatus.Rejected;
        ticket.ValidatedByUserId = actingUserId;
        ticket.ValidatedDate = DateTime.UtcNow;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _ticketRepository.UpdateAsync(ticket);

        if (approve)
        {
            await IncrementInventoryAsync(ticket);
        }

        return MapToDto(ticket);
    }

    public async Task DeleteAsync(int id)
    {
        var ticket =
            await _ticketRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"ProductIncorporationTicket {id} not found.");

        if (ticket.Status != ProductIncorporationTicketStatus.PendingValidation)
        {
            throw new ValidationAppException(
                "Solo se puede eliminar un ticket en estado PendingValidation."
            );
        }

        await _ticketRepository.DeleteAsync(ticket);
    }

    public async Task<ProductIncorporationTicketDto> GetByIdAsync(int id)
    {
        var ticket =
            await _ticketRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"ProductIncorporationTicket {id} not found.");
        return MapToDto(ticket);
    }

    public async Task<PagedResult<ProductIncorporationTicketDto>> GetAllByBranchAsync(
        int branchId,
        int pageNumber,
        int pageSize
    )
    {
        var (items, totalCount) = await _ticketRepository.GetPagedByBranchAsync(
            branchId,
            pageNumber,
            pageSize
        );

        return new PagedResult<ProductIncorporationTicketDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
    }

    private async Task IncrementInventoryAsync(ProductIncorporationTicket ticket)
    {
        var inventory = await _inventoryRepository.GetByBranchAndProductAsync(
            ticket.BranchId,
            ticket.ProductId
        );

        if (inventory is null)
        {
            inventory = new BranchInventory
            {
                BranchId = ticket.BranchId,
                ProductId = ticket.ProductId,
                Quantity = ticket.Quantity,
                MinimumStock = 0,
                CreatedAt = DateTime.UtcNow,
            };
            await _inventoryRepository.AddAsync(inventory);
        }
        else
        {
            inventory.Quantity += ticket.Quantity;
            inventory.UpdatedAt = DateTime.UtcNow;
            await _inventoryRepository.UpdateAsync(inventory);
        }
    }

    private static ProductIncorporationTicketDto MapToDto(ProductIncorporationTicket ticket) =>
        new()
        {
            Id = ticket.Id,
            BranchId = ticket.BranchId,
            ProductId = ticket.ProductId,
            SupplierId = ticket.SupplierId,
            Quantity = ticket.Quantity,
            UnitCost = ticket.UnitCost,
            CreatedByUserId = ticket.CreatedByUserId,
            CreatedDate = ticket.CreatedDate,
            Status = ticket.Status,
            ValidatedByUserId = ticket.ValidatedByUserId,
            ValidatedDate = ticket.ValidatedDate,
        };
}
