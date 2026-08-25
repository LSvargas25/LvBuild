using FluentValidation;
using LvApplication.Common;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.SiteLogs;
using LvApplication.Services.Budgets;
using LvApplication.Services.Inventory;
using LvApplication.Services.Progress;
using LvApplication.Services.Projects;
using LvDomain.Entities.Inventory;
using LvDomain.Entities.SiteLogs;
using LvDomain.Enums;

namespace LvApplication.Services.SiteLogs;

public class SiteLogService : ISiteLogService
{
    private readonly ISiteLogRepository _siteLogRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectInventoryItemRepository _inventoryRepository;
    private readonly IBudgetRepository _budgetRepository;
    private readonly IProjectProgressService _projectProgressService;
    private readonly IValidator<CreateSiteLogDto> _createValidator;
    private readonly IValidator<UpdateSiteLogDto> _updateValidator;

    public SiteLogService(
        ISiteLogRepository siteLogRepository,
        IProjectRepository projectRepository,
        IProjectInventoryItemRepository inventoryRepository,
        IBudgetRepository budgetRepository,
        IProjectProgressService projectProgressService,
        IValidator<CreateSiteLogDto> createValidator,
        IValidator<UpdateSiteLogDto> updateValidator
    )
    {
        _siteLogRepository = siteLogRepository;
        _projectRepository = projectRepository;
        _inventoryRepository = inventoryRepository;
        _budgetRepository = budgetRepository;
        _projectProgressService = projectProgressService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<SiteLogDto> CreateAsync(CreateSiteLogDto request, int createdByUserId)
    {
        await _createValidator.ValidateAndThrowAppExceptionAsync(request);

        var project =
            await _projectRepository.GetByIdAsync(request.ProjectId)
            ?? throw new NotFoundException($"Project {request.ProjectId} not found.");

        if (await _siteLogRepository.ExistsForProjectAndWeekAsync(project.Id, request.WeekStart))
        {
            throw new ConflictException("Ya existe una bitácora para este proyecto en esa semana.");
        }

        await ValidateChapterAsync(project.BudgetId, request.ChapterId);

        var siteLog = new SiteLog
        {
            ProjectId = project.Id,
            WeekStart = request.WeekStart,
            WeekEnd = request.WeekEnd,
            TaskDescription = request.TaskDescription,
            PendingTasks = request.PendingTasks,
            Status = SiteLogStatus.Draft,
            ChapterId = request.ChapterId,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
        };

        SyncWorkers(siteLog, request.Workers);
        SyncMaterials(siteLog, request.Materials);
        SyncEquipment(siteLog, request.Equipment);

        await _siteLogRepository.AddAsync(siteLog);

        return MapToDto(siteLog);
    }

    public async Task<SiteLogDto> UpdateAsync(int id, UpdateSiteLogDto request)
    {
        await _updateValidator.ValidateAndThrowAppExceptionAsync(request);

        var siteLog =
            await _siteLogRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"SiteLog {id} not found.");

        EnsureEditable(siteLog);

        if (request.ChapterId.HasValue)
        {
            var project =
                await _projectRepository.GetByIdAsync(siteLog.ProjectId)
                ?? throw new NotFoundException($"Project {siteLog.ProjectId} not found.");
            await ValidateChapterAsync(project.BudgetId, request.ChapterId);
        }

        siteLog.ChapterId = request.ChapterId;
        siteLog.TaskDescription = request.TaskDescription;
        siteLog.PendingTasks = request.PendingTasks;
        siteLog.UpdatedAt = DateTime.UtcNow;

        SyncWorkers(siteLog, request.Workers);
        SyncMaterials(siteLog, request.Materials);
        SyncEquipment(siteLog, request.Equipment);

        await _siteLogRepository.UpdateAsync(siteLog);

        return MapToDto(siteLog);
    }

    public async Task<SiteLogDto> SubmitToReviewAsync(int id)
    {
        var siteLog =
            await _siteLogRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"SiteLog {id} not found.");

        if (siteLog.Status != SiteLogStatus.Draft)
        {
            throw new ValidationAppException(
                "Solo se puede enviar a revisión una bitácora en Borrador."
            );
        }

        siteLog.Status = SiteLogStatus.Review;
        siteLog.UpdatedAt = DateTime.UtcNow;
        await _siteLogRepository.UpdateAsync(siteLog);

        return MapToDto(siteLog);
    }

    public async Task<SiteLogDto> RevertToDraftAsync(int id, RevertToDraftDto request)
    {
        var siteLog =
            await _siteLogRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"SiteLog {id} not found.");

        if (siteLog.Status != SiteLogStatus.Review)
        {
            throw new ValidationAppException(
                "Solo se puede devolver a Borrador una bitácora en Revisión."
            );
        }

        siteLog.Status = SiteLogStatus.Draft;
        siteLog.UpdatedAt = DateTime.UtcNow;
        await _siteLogRepository.UpdateAsync(siteLog);

        return MapToDto(siteLog);
    }

    public async Task<SiteLogDto> ApproveAsync(int id, int approvedByUserId)
    {
        var siteLog =
            await _siteLogRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"SiteLog {id} not found.");

        if (siteLog.Status != SiteLogStatus.Review)
        {
            throw new ValidationAppException(
                "Solo se puede aprobar una bitácora en estado Revisión."
            );
        }

        var project =
            await _projectRepository.GetByIdAsync(siteLog.ProjectId)
            ?? throw new NotFoundException($"Project {siteLog.ProjectId} not found.");

        // First pass: validate every material against inventory before mutating anything,
        // so a shortfall on one material rejects the whole approval instead of leaving
        // the inventory partially consumed.
        var consumptions =
            new List<(SiteLogMaterial SiteLogMaterial, ProjectInventoryItem InventoryItem)>();
        foreach (var siteLogMaterial in siteLog.Materials)
        {
            var inventoryItem = await _inventoryRepository.GetByProjectAndMaterialAsync(
                siteLog.ProjectId,
                siteLogMaterial.MaterialId
            );

            if (
                inventoryItem is null
                || inventoryItem.CurrentQuantity - siteLogMaterial.QuantityUsed < 0
            )
            {
                throw new ValidationAppException(
                    $"No hay suficiente inventario del material {siteLogMaterial.MaterialId} para aprobar esta bitácora."
                );
            }

            consumptions.Add((siteLogMaterial, inventoryItem));
        }

        decimal totalMaterials = 0;
        foreach (var (siteLogMaterial, inventoryItem) in consumptions)
        {
            totalMaterials += siteLogMaterial.QuantityUsed * inventoryItem.ReferenceUnitCost;

            inventoryItem.CurrentQuantity -= siteLogMaterial.QuantityUsed;
            inventoryItem.UpdatedAt = DateTime.UtcNow;
            await _inventoryRepository.UpdateAsync(inventoryItem);
        }

        siteLog.TotalMaterials = totalMaterials;
        siteLog.Status = SiteLogStatus.Approved;
        siteLog.ApprovedByUserId = approvedByUserId;
        siteLog.UpdatedAt = DateTime.UtcNow;

        var progress = await _projectProgressService.CalculateAndRecordAsync(siteLog.Id);
        siteLog.ProgressPercentage = progress.ProgressPercentage;

        await _siteLogRepository.UpdateAsync(siteLog);

        project.TotalWorkedHours += siteLog.Workers.Sum(w => w.HoursWorked);
        project.UpdatedAt = DateTime.UtcNow;
        await _projectRepository.UpdateAsync(project);

        return MapToDto(siteLog);
    }

    public async Task DeleteAsync(int id)
    {
        var siteLog =
            await _siteLogRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"SiteLog {id} not found.");

        if (siteLog.Status != SiteLogStatus.Draft)
        {
            throw new ValidationAppException(
                "Solo se puede eliminar una bitácora en estado Borrador."
            );
        }

        await _siteLogRepository.DeleteAsync(siteLog);
    }

    public async Task<SiteLogDto> GetByIdAsync(int id)
    {
        var siteLog =
            await _siteLogRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"SiteLog {id} not found.");
        return MapToDto(siteLog);
    }

    public async Task<PagedResult<SiteLogDto>> GetAllAsync(int pageNumber, int pageSize)
    {
        var (items, totalCount) = await _siteLogRepository.GetPagedAsync(pageNumber, pageSize);

        return new PagedResult<SiteLogDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
    }

    public async Task<PagedResult<SiteLogDto>> GetAllByProjectAsync(
        int projectId,
        int pageNumber,
        int pageSize
    )
    {
        var (items, totalCount) = await _siteLogRepository.GetPagedByProjectAsync(
            projectId,
            pageNumber,
            pageSize
        );

        return new PagedResult<SiteLogDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
    }

    public async Task UpdateTotalPayrollAsync(int siteLogId, decimal amount)
    {
        var siteLog =
            await _siteLogRepository.GetByIdAsync(siteLogId)
            ?? throw new NotFoundException($"SiteLog {siteLogId} not found.");

        siteLog.TotalPayroll = amount;
        siteLog.UpdatedAt = DateTime.UtcNow;
        await _siteLogRepository.UpdateAsync(siteLog);
    }

    private static void EnsureEditable(SiteLog siteLog)
    {
        if (siteLog.Status == SiteLogStatus.Approved)
        {
            throw new ValidationAppException("No se puede editar una bitácora Aprobada.");
        }
    }

    private static void SyncWorkers(SiteLog siteLog, List<SiteLogWorkerDto> workerDtos)
    {
        var incomingWorkerIds = workerDtos.Select(w => w.WorkerId).ToHashSet();
        var toRemove = siteLog.Workers.Where(w => !incomingWorkerIds.Contains(w.WorkerId)).ToList();
        foreach (var worker in toRemove)
        {
            siteLog.Workers.Remove(worker);
        }

        foreach (var dto in workerDtos)
        {
            var worker = siteLog.Workers.FirstOrDefault(w => w.WorkerId == dto.WorkerId);
            if (worker is null)
            {
                worker = new SiteLogWorker { WorkerId = dto.WorkerId, CreatedAt = DateTime.UtcNow };
                siteLog.Workers.Add(worker);
            }

            worker.HoursWorked = dto.HoursWorked;
            worker.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static void SyncMaterials(SiteLog siteLog, List<SiteLogMaterialDto> materialDtos)
    {
        var incomingMaterialIds = materialDtos.Select(m => m.MaterialId).ToHashSet();
        var toRemove = siteLog
            .Materials.Where(m => !incomingMaterialIds.Contains(m.MaterialId))
            .ToList();
        foreach (var material in toRemove)
        {
            siteLog.Materials.Remove(material);
        }

        foreach (var dto in materialDtos)
        {
            var material = siteLog.Materials.FirstOrDefault(m => m.MaterialId == dto.MaterialId);
            if (material is null)
            {
                material = new SiteLogMaterial
                {
                    MaterialId = dto.MaterialId,
                    CreatedAt = DateTime.UtcNow,
                };
                siteLog.Materials.Add(material);
            }

            material.QuantityUsed = dto.QuantityUsed;
            material.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static void SyncEquipment(SiteLog siteLog, List<SiteLogEquipmentDto> equipmentDtos)
    {
        siteLog.Equipment.Clear();

        foreach (var dto in equipmentDtos)
        {
            siteLog.Equipment.Add(
                new SiteLogEquipment
                {
                    EquipmentType = dto.EquipmentType,
                    Description = dto.Description,
                    CreatedAt = DateTime.UtcNow,
                }
            );
        }
    }

    private static SiteLogDto MapToDto(SiteLog siteLog) =>
        new()
        {
            Id = siteLog.Id,
            ProjectId = siteLog.ProjectId,
            ChapterId = siteLog.ChapterId,
            WeekStart = siteLog.WeekStart,
            WeekEnd = siteLog.WeekEnd,
            TaskDescription = siteLog.TaskDescription,
            PendingTasks = siteLog.PendingTasks,
            TotalPayroll = siteLog.TotalPayroll,
            TotalMaterials = siteLog.TotalMaterials,
            ProgressPercentage = siteLog.ProgressPercentage,
            Status = siteLog.Status,
            CreatedByUserId = siteLog.CreatedByUserId,
            ApprovedByUserId = siteLog.ApprovedByUserId,
            Workers = siteLog
                .Workers.Select(w => new SiteLogWorkerResponseDto
                {
                    Id = w.Id,
                    WorkerId = w.WorkerId,
                    HoursWorked = w.HoursWorked,
                })
                .ToList(),
            Materials = siteLog
                .Materials.Select(m => new SiteLogMaterialResponseDto
                {
                    Id = m.Id,
                    MaterialId = m.MaterialId,
                    QuantityUsed = m.QuantityUsed,
                })
                .ToList(),
            Equipment = siteLog
                .Equipment.Select(e => new SiteLogEquipmentResponseDto
                {
                    Id = e.Id,
                    EquipmentType = e.EquipmentType,
                    Description = e.Description,
                })
                .ToList(),
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
