using FluentValidation;
using LvApplication.Common;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Inventory;
using LvApplication.Services.Budgets;
using LvApplication.Services.Materials;
using LvApplication.Services.Projects;
using LvApplication.Services.Suppliers;
using LvDomain.Entities.Inventory;
using LvDomain.Enums;

namespace LvApplication.Services.Inventory;

public class MaterialTicketService : IMaterialTicketService
{
    private readonly IMaterialTicketRepository _ticketRepository;
    private readonly IProjectInventoryItemRepository _inventoryRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IBudgetRepository _budgetRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IMaterialCatalogRepository _materialCatalogRepository;
    private readonly IProjectChapterService _projectChapterService;
    private readonly IValidator<CreateMaterialTicketDto> _createValidator;
    private readonly IValidator<UpdateMaterialTicketDto> _updateValidator;

    public MaterialTicketService(
        IMaterialTicketRepository ticketRepository,
        IProjectInventoryItemRepository inventoryRepository,
        IProjectRepository projectRepository,
        IBudgetRepository budgetRepository,
        ISupplierRepository supplierRepository,
        IMaterialCatalogRepository materialCatalogRepository,
        IProjectChapterService projectChapterService,
        IValidator<CreateMaterialTicketDto> createValidator,
        IValidator<UpdateMaterialTicketDto> updateValidator
    )
    {
        _ticketRepository = ticketRepository;
        _inventoryRepository = inventoryRepository;
        _projectRepository = projectRepository;
        _budgetRepository = budgetRepository;
        _supplierRepository = supplierRepository;
        _materialCatalogRepository = materialCatalogRepository;
        _projectChapterService = projectChapterService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<MaterialTicketDto> CreateAsync(
        int projectId,
        CreateMaterialTicketDto request,
        int createdByUserId
    )
    {
        await _createValidator.ValidateAndThrowAppExceptionAsync(request);

        var project =
            await _projectRepository.GetByIdAsync(projectId)
            ?? throw new NotFoundException($"Project {projectId} not found.");

        await ValidateChapterAsync(project.BudgetId, request.ChapterId);

        var supplier =
            await _supplierRepository.GetByIdAsync(request.SupplierId)
            ?? throw new NotFoundException($"Supplier {request.SupplierId} not found.");

        var material =
            await _materialCatalogRepository.GetByIdAsync(request.MaterialId)
            ?? throw new NotFoundException($"Material {request.MaterialId} not found.");

        var subtotal = request.Quantity * request.UnitPrice;
        var total = subtotal - (request.Discount ?? 0);

        var ticket = new MaterialTicket
        {
            ProjectId = project.Id,
            SupplierId = supplier.Id,
            MaterialId = material.Id,
            CreatedByUserId = createdByUserId,
            Description = request.Description,
            InvoicePhotoPath = request.InvoicePhotoPath,
            MaterialName = material.Name,
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            Discount = request.Discount,
            Subtotal = subtotal,
            Total = total,
            Status = MaterialTicketStatus.Review,
            ChapterId = request.ChapterId,
            CreatedAt = DateTime.UtcNow,
        };

        await _ticketRepository.AddAsync(ticket);

        project.PendingExpenses += total;
        project.UpdatedAt = DateTime.UtcNow;
        await _projectRepository.UpdateAsync(project);

        return MapToDto(ticket);
    }

    public async Task<MaterialTicketDto> UpdateAsync(int id, UpdateMaterialTicketDto request)
    {
        await _updateValidator.ValidateAndThrowAppExceptionAsync(request);

        var ticket =
            await _ticketRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"MaterialTicket {id} not found.");

        if (ticket.Status != MaterialTicketStatus.Review)
        {
            throw new ValidationAppException("Solo se puede editar un ticket en estado Revisión.");
        }

        var project =
            await _projectRepository.GetByIdAsync(ticket.ProjectId)
            ?? throw new NotFoundException($"Project {ticket.ProjectId} not found.");

        await ValidateChapterAsync(project.BudgetId, request.ChapterId);

        var supplier =
            await _supplierRepository.GetByIdAsync(request.SupplierId)
            ?? throw new NotFoundException($"Supplier {request.SupplierId} not found.");

        var material =
            await _materialCatalogRepository.GetByIdAsync(request.MaterialId)
            ?? throw new NotFoundException($"Material {request.MaterialId} not found.");

        var previousTotal = ticket.Total;

        ticket.SupplierId = supplier.Id;
        ticket.MaterialId = material.Id;
        ticket.MaterialName = material.Name;
        ticket.Description = request.Description;
        ticket.InvoicePhotoPath = request.InvoicePhotoPath;
        ticket.Quantity = request.Quantity;
        ticket.UnitPrice = request.UnitPrice;
        ticket.Discount = request.Discount;
        ticket.Subtotal = request.Quantity * request.UnitPrice;
        ticket.Total = ticket.Subtotal - (request.Discount ?? 0);
        ticket.ChapterId = request.ChapterId;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _ticketRepository.UpdateAsync(ticket);

        project.PendingExpenses += ticket.Total - previousTotal;
        project.UpdatedAt = DateTime.UtcNow;
        await _projectRepository.UpdateAsync(project);

        return MapToDto(ticket);
    }

    public async Task<MaterialTicketDto> ApplyAsync(int id)
    {
        var ticket =
            await _ticketRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"MaterialTicket {id} not found.");

        if (ticket.Status != MaterialTicketStatus.Review)
        {
            throw new ValidationAppException("Solo se puede aplicar un ticket en estado Revisión.");
        }

        ticket.Status = MaterialTicketStatus.Applied;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _ticketRepository.UpdateAsync(ticket);

        var project =
            await _projectRepository.GetByIdAsync(ticket.ProjectId)
            ?? throw new NotFoundException($"Project {ticket.ProjectId} not found.");

        project.PendingExpenses -= ticket.Total;
        project.CurrentDirectExpenses += ticket.Total;

        var inventoryItem = await _inventoryRepository.GetByProjectAndMaterialAsync(
            ticket.ProjectId,
            ticket.MaterialId
        );

        if (inventoryItem is null)
        {
            inventoryItem = new ProjectInventoryItem
            {
                ProjectId = ticket.ProjectId,
                MaterialId = ticket.MaterialId,
                CurrentQuantity = ticket.Quantity,
                ReferenceUnitCost = ticket.UnitPrice,
                CreatedAt = DateTime.UtcNow,
            };
            await _inventoryRepository.AddAsync(inventoryItem);
        }
        else
        {
            inventoryItem.CurrentQuantity += ticket.Quantity;
            inventoryItem.ReferenceUnitCost = ticket.UnitPrice;
            inventoryItem.UpdatedAt = DateTime.UtcNow;
            await _inventoryRepository.UpdateAsync(inventoryItem);
        }

        project.MaterialsUsedCount = await _inventoryRepository.CountWithQuantityAsync(
            ticket.ProjectId
        );
        project.UpdatedAt = DateTime.UtcNow;
        await _projectRepository.UpdateAsync(project);

        if (ticket.ChapterId.HasValue)
        {
            await _projectChapterService.RecalculateActualCostAsync(
                ticket.ProjectId,
                ticket.ChapterId.Value
            );
        }

        return MapToDto(ticket);
    }

    public async Task<MaterialTicketDto> ArchiveAsync(int id)
    {
        var ticket =
            await _ticketRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"MaterialTicket {id} not found.");

        if (ticket.Status != MaterialTicketStatus.Review)
        {
            throw new ValidationAppException(
                "Solo se puede archivar un ticket en estado Revisión."
            );
        }

        ticket.Status = MaterialTicketStatus.Archived;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _ticketRepository.UpdateAsync(ticket);

        var project =
            await _projectRepository.GetByIdAsync(ticket.ProjectId)
            ?? throw new NotFoundException($"Project {ticket.ProjectId} not found.");

        project.PendingExpenses -= ticket.Total;
        project.UpdatedAt = DateTime.UtcNow;
        await _projectRepository.UpdateAsync(project);

        return MapToDto(ticket);
    }

    public async Task DeleteAsync(int id)
    {
        var ticket =
            await _ticketRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"MaterialTicket {id} not found.");

        if (ticket.Status != MaterialTicketStatus.Review)
        {
            throw new ValidationAppException(
                "Solo se puede eliminar un ticket en estado Revisión; una vez aplicado o archivado queda como registro histórico."
            );
        }

        var project =
            await _projectRepository.GetByIdAsync(ticket.ProjectId)
            ?? throw new NotFoundException($"Project {ticket.ProjectId} not found.");

        project.PendingExpenses -= ticket.Total;
        project.UpdatedAt = DateTime.UtcNow;
        await _projectRepository.UpdateAsync(project);

        await _ticketRepository.DeleteAsync(ticket);
    }

    public async Task<MaterialTicketDto> GetByIdAsync(int id)
    {
        var ticket =
            await _ticketRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"MaterialTicket {id} not found.");
        return MapToDto(ticket);
    }

    public async Task<PagedResult<MaterialTicketDto>> GetAllByProjectAsync(
        int projectId,
        int pageNumber,
        int pageSize
    )
    {
        var (items, totalCount) = await _ticketRepository.GetPagedByProjectAsync(
            projectId,
            pageNumber,
            pageSize
        );

        return new PagedResult<MaterialTicketDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
    }

    public async Task<List<ProjectInventoryItemDto>> GetInventoryAsync(int projectId)
    {
        var items = await _inventoryRepository.GetByProjectAsync(projectId);

        return items
            .Select(i => new ProjectInventoryItemDto
            {
                Id = i.Id,
                ProjectId = i.ProjectId,
                MaterialId = i.MaterialId,
                ProductId = i.ProductId,
                CurrentQuantity = i.CurrentQuantity,
                ReferenceUnitCost = i.ReferenceUnitCost,
            })
            .ToList();
    }

    private static MaterialTicketDto MapToDto(MaterialTicket ticket) =>
        new()
        {
            Id = ticket.Id,
            ProjectId = ticket.ProjectId,
            SupplierId = ticket.SupplierId,
            MaterialId = ticket.MaterialId,
            CreatedByUserId = ticket.CreatedByUserId,
            CreatedAt = ticket.CreatedAt,
            Description = ticket.Description,
            InvoicePhotoPath = ticket.InvoicePhotoPath,
            MaterialName = ticket.MaterialName,
            Quantity = ticket.Quantity,
            UnitPrice = ticket.UnitPrice,
            Discount = ticket.Discount,
            Subtotal = ticket.Subtotal,
            Total = ticket.Total,
            Status = ticket.Status,
            ChapterId = ticket.ChapterId,
        };

    private async Task ValidateChapterAsync(int budgetId, int? chapterId)
    {
        if (!chapterId.HasValue)
        {
            return;
        }

        var budget =
            await _budgetRepository.GetByIdAsync(budgetId)
            ?? throw new NotFoundException($"Budget {budgetId} not found.");

        if (!budget.Chapters.Any(c => c.Id == chapterId.Value))
        {
            throw new ValidationAppException(
                $"El capítulo {chapterId} no pertenece al presupuesto de este proyecto."
            );
        }
    }
}
