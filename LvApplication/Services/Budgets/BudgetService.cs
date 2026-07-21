using FluentValidation;
using LvApplication.Common;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Budgets;
using LvDomain.Entities.Budgets;
using LvDomain.Enums;

namespace LvApplication.Services.Budgets;

public class BudgetService : IBudgetService
{
    private readonly IBudgetRepository _budgetRepository;
    private readonly IValidator<CreateBudgetDto> _createValidator;
    private readonly IValidator<UpdateBudgetDto> _updateValidator;
    private readonly IValidator<RequestCorrectionDto> _requestCorrectionValidator;
    private readonly IValidator<CancelBudgetDto> _cancelValidator;

    public BudgetService(
        IBudgetRepository budgetRepository,
        IValidator<CreateBudgetDto> createValidator,
        IValidator<UpdateBudgetDto> updateValidator,
        IValidator<RequestCorrectionDto> requestCorrectionValidator,
        IValidator<CancelBudgetDto> cancelValidator)
    {
        _budgetRepository = budgetRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _requestCorrectionValidator = requestCorrectionValidator;
        _cancelValidator = cancelValidator;
    }

    public async Task<BudgetResponseDto> CreateAsync(CreateBudgetDto request, int createdByUserId)
    {
        await _createValidator.ValidateAndThrowAppExceptionAsync(request);

        var budget = new Budget
        {
            CustomerId = request.CustomerId,
            BranchId = request.BranchId,
            Name = request.Name,
            Status = BudgetStatus.Draft,
            UtilityPercentage = request.UtilityPercentage,
            IndirectCostsTotal = request.IndirectCostsTotal,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        SyncChapters(budget, request.Chapters);
        RecalculateTotals(budget);

        budget.History.Add(new BudgetHistory
        {
            UserId = createdByUserId,
            PreviousStatus = null,
            NewStatus = BudgetStatus.Draft,
            Timestamp = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });

        await _budgetRepository.AddAsync(budget);

        return MapToDto(budget);
    }

    public async Task<BudgetResponseDto> UpdateAsync(int id, UpdateBudgetDto request)
    {
        await _updateValidator.ValidateAndThrowAppExceptionAsync(request);

        var budget = await _budgetRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Budget {id} not found.");

        EnsureEditable(budget);

        budget.CustomerId = request.CustomerId;
        budget.BranchId = request.BranchId;
        budget.Name = request.Name;
        budget.UtilityPercentage = request.UtilityPercentage;
        budget.IndirectCostsTotal = request.IndirectCostsTotal;
        budget.UpdatedAt = DateTime.UtcNow;

        SyncChapters(budget, request.Chapters);
        RecalculateTotals(budget);

        await _budgetRepository.UpdateAsync(budget);

        return MapToDto(budget);
    }

    public async Task<BudgetResponseDto> SubmitForReviewAsync(int id, int actingUserId)
    {
        var budget = await _budgetRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Budget {id} not found.");

        if (budget.Status != BudgetStatus.Draft && budget.Status != BudgetStatus.Correction)
        {
            throw new ForbiddenException("Solo se puede enviar a revisión un presupuesto en Borrador o Corrección.");
        }

        if (!budget.Chapters.Any())
        {
            throw new ValidationAppException("El presupuesto debe tener al menos un capítulo para enviarse a revisión.");
        }

        await TransitionAsync(budget, BudgetStatus.Review, actingUserId, comment: null, reason: null);

        return MapToDto(budget);
    }

    public async Task<BudgetResponseDto> ApproveInternalAsync(int id, int actingUserId)
    {
        var budget = await _budgetRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Budget {id} not found.");

        if (budget.Status != BudgetStatus.Review)
        {
            throw new ForbiddenException("Solo se puede aprobar internamente un presupuesto en Revisión.");
        }

        await TransitionAsync(budget, BudgetStatus.Sent, actingUserId, comment: null, reason: null);

        return MapToDto(budget);
    }

    public async Task<BudgetResponseDto> RequestCorrectionAsync(int id, RequestCorrectionDto request, int actingUserId)
    {
        await _requestCorrectionValidator.ValidateAndThrowAppExceptionAsync(request);

        var budget = await _budgetRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Budget {id} not found.");

        if (budget.Status != BudgetStatus.Review)
        {
            throw new ForbiddenException("Solo se puede solicitar corrección de un presupuesto en Revisión.");
        }

        await TransitionAsync(budget, BudgetStatus.Correction, actingUserId, comment: request.Comment, reason: null);

        return MapToDto(budget);
    }

    public async Task<BudgetResponseDto> WithdrawFromCommercialAsync(int id, RequestCorrectionDto request, int actingUserId)
    {
        await _requestCorrectionValidator.ValidateAndThrowAppExceptionAsync(request);

        var budget = await _budgetRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Budget {id} not found.");

        if (budget.Status != BudgetStatus.Sent)
        {
            throw new ForbiddenException("Solo se puede retirar del proceso comercial un presupuesto Enviado.");
        }

        await TransitionAsync(budget, BudgetStatus.Correction, actingUserId, comment: request.Comment, reason: null);

        return MapToDto(budget);
    }

    public async Task<BudgetResponseDto> MarkClientApprovedAsync(int id, int actingUserId)
    {
        var budget = await _budgetRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Budget {id} not found.");

        if (budget.Status != BudgetStatus.Sent)
        {
            throw new ForbiddenException("Solo se puede marcar como aprobado por el cliente un presupuesto Enviado.");
        }

        await TransitionAsync(budget, BudgetStatus.ClientApproved, actingUserId, comment: null, reason: null);

        return MapToDto(budget);
    }

    public async Task<BudgetResponseDto> CancelAsync(int id, CancelBudgetDto request, int actingUserId)
    {
        await _cancelValidator.ValidateAndThrowAppExceptionAsync(request);

        var budget = await _budgetRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Budget {id} not found.");

        if (budget.Status is BudgetStatus.ClientApproved or BudgetStatus.Cancelled)
        {
            throw new ValidationAppException($"No se puede cancelar un presupuesto en estado {budget.Status}.");
        }

        await TransitionAsync(budget, BudgetStatus.Cancelled, actingUserId, comment: null, reason: request.Reason);

        return MapToDto(budget);
    }

    public async Task DeleteAsync(int id)
    {
        var budget = await _budgetRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Budget {id} not found.");

        if (budget.Status != BudgetStatus.Draft)
        {
            throw new ValidationAppException("Solo se puede eliminar un presupuesto en estado Borrador; use Cancelar en cualquier otro estado.");
        }

        // The creation entry always has PreviousStatus == null; any entry with a non-null
        // PreviousStatus means the budget actually transitioned away from Draft at some point
        // (even if it later came back to Draft via Correction), which disqualifies hard delete.
        var history = await _budgetRepository.GetHistoryAsync(id);
        if (history.Any(h => h.PreviousStatus.HasValue))
        {
            throw new ValidationAppException("Este presupuesto ya tuvo actividad (salió de Borrador alguna vez); use Cancelar en vez de Eliminar.");
        }

        await _budgetRepository.DeleteAsync(budget);
    }

    public async Task<BudgetResponseDto> GetByIdAsync(int id)
    {
        var budget = await _budgetRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Budget {id} not found.");
        return MapToDto(budget);
    }

    public async Task<PagedResult<BudgetResponseDto>> GetAllAsync(int pageNumber, int pageSize, BudgetStatus? status)
    {
        var (items, totalCount) = await _budgetRepository.GetPagedAsync(pageNumber, pageSize, status);

        return new PagedResult<BudgetResponseDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<List<BudgetHistoryResponseDto>> GetHistoryAsync(int budgetId)
    {
        var history = await _budgetRepository.GetHistoryAsync(budgetId);

        return history
            .OrderBy(h => h.Timestamp)
            .Select(h => new BudgetHistoryResponseDto
            {
                Id = h.Id,
                BudgetId = h.BudgetId,
                UserId = h.UserId,
                PreviousStatus = h.PreviousStatus,
                NewStatus = h.NewStatus,
                Comment = h.Comment,
                Reason = h.Reason,
                Timestamp = h.Timestamp
            })
            .ToList();
    }

    private static void EnsureEditable(Budget budget)
    {
        if (budget.Status != BudgetStatus.Draft && budget.Status != BudgetStatus.Correction)
        {
            throw new ForbiddenException("No se puede editar un presupuesto en este estado");
        }
    }

    private async Task TransitionAsync(Budget budget, BudgetStatus newStatus, int actingUserId, string? comment, string? reason)
    {
        var previousStatus = budget.Status;
        budget.Status = newStatus;
        budget.UpdatedAt = DateTime.UtcNow;

        budget.History.Add(new BudgetHistory
        {
            UserId = actingUserId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Comment = comment,
            Reason = reason,
            Timestamp = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });

        await _budgetRepository.UpdateAsync(budget);
    }

    private static void RecalculateTotals(Budget budget)
    {
        foreach (var chapter in budget.Chapters)
        {
            chapter.TotalChapter = chapter.Activities.Sum(a => a.TotalActivity);
        }

        var chaptersTotal = budget.Chapters.Sum(c => c.TotalChapter);
        budget.TotalBudget = (chaptersTotal + budget.IndirectCostsTotal) * (1 + budget.UtilityPercentage / 100m);
    }

    private static void SyncChapters(Budget budget, List<BudgetChapterDto> chapterDtos)
    {
        var incomingIds = chapterDtos.Where(c => c.Id.HasValue).Select(c => c.Id!.Value).ToHashSet();
        var toRemove = budget.Chapters.Where(c => !incomingIds.Contains(c.Id)).ToList();
        foreach (var chapter in toRemove)
        {
            budget.Chapters.Remove(chapter);
        }

        foreach (var dto in chapterDtos)
        {
            var chapter = dto.Id.HasValue
                ? budget.Chapters.First(c => c.Id == dto.Id.Value)
                : NewChapter(budget);

            chapter.Name = dto.Name;
            chapter.Order = dto.Order;
            chapter.EstimatedWeeks = dto.EstimatedWeeks;
            chapter.UpdatedAt = DateTime.UtcNow;

            SyncActivities(chapter, dto.Activities);
        }
    }

    private static BudgetChapter NewChapter(Budget budget)
    {
        var chapter = new BudgetChapter { CreatedAt = DateTime.UtcNow };
        budget.Chapters.Add(chapter);
        return chapter;
    }

    private static void SyncActivities(BudgetChapter chapter, List<BudgetActivityDto> activityDtos)
    {
        var incomingIds = activityDtos.Where(a => a.Id.HasValue).Select(a => a.Id!.Value).ToHashSet();
        var toRemove = chapter.Activities.Where(a => !incomingIds.Contains(a.Id)).ToList();
        foreach (var activity in toRemove)
        {
            chapter.Activities.Remove(activity);
        }

        foreach (var dto in activityDtos)
        {
            var activity = dto.Id.HasValue
                ? chapter.Activities.First(a => a.Id == dto.Id.Value)
                : NewActivity(chapter);

            activity.Description = dto.Description;
            activity.MaterialQuantity = dto.MaterialQuantity;
            activity.MaterialCost = dto.MaterialCost;
            activity.LaborCost = dto.LaborCost;
            activity.EquipmentCost = dto.EquipmentCost;
            activity.TotalActivity = dto.MaterialCost + dto.LaborCost + dto.EquipmentCost;
            activity.UpdatedAt = DateTime.UtcNow;

            SyncMaterials(activity, dto.Materials);
            SyncEquipment(activity, dto.Equipment);
            SyncLabor(activity, dto.Labor);
        }
    }

    private static BudgetActivity NewActivity(BudgetChapter chapter)
    {
        var activity = new BudgetActivity { CreatedAt = DateTime.UtcNow };
        chapter.Activities.Add(activity);
        return activity;
    }

    private static void SyncMaterials(BudgetActivity activity, List<BudgetActivityMaterialDto> materialDtos)
    {
        var incomingIds = materialDtos.Where(m => m.Id.HasValue).Select(m => m.Id!.Value).ToHashSet();
        var toRemove = activity.Materials.Where(m => !incomingIds.Contains(m.Id)).ToList();
        foreach (var material in toRemove)
        {
            activity.Materials.Remove(material);
        }

        foreach (var dto in materialDtos)
        {
            var material = dto.Id.HasValue
                ? activity.Materials.First(m => m.Id == dto.Id.Value)
                : NewMaterial(activity);

            material.MaterialId = dto.MaterialId;
            material.UnitPrice = dto.UnitPrice;
            material.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static BudgetActivityMaterial NewMaterial(BudgetActivity activity)
    {
        var material = new BudgetActivityMaterial { CreatedAt = DateTime.UtcNow };
        activity.Materials.Add(material);
        return material;
    }

    private static void SyncEquipment(BudgetActivity activity, List<BudgetActivityEquipmentDto> equipmentDtos)
    {
        var incomingIds = equipmentDtos.Where(e => e.Id.HasValue).Select(e => e.Id!.Value).ToHashSet();
        var toRemove = activity.Equipment.Where(e => !incomingIds.Contains(e.Id)).ToList();
        foreach (var equipment in toRemove)
        {
            activity.Equipment.Remove(equipment);
        }

        foreach (var dto in equipmentDtos)
        {
            var equipment = dto.Id.HasValue
                ? activity.Equipment.First(e => e.Id == dto.Id.Value)
                : NewEquipment(activity);

            equipment.EquipmentName = dto.EquipmentName;
            equipment.UnitPrice = dto.UnitPrice;
            equipment.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static BudgetActivityEquipment NewEquipment(BudgetActivity activity)
    {
        var equipment = new BudgetActivityEquipment { CreatedAt = DateTime.UtcNow };
        activity.Equipment.Add(equipment);
        return equipment;
    }

    private static void SyncLabor(BudgetActivity activity, List<BudgetActivityLaborDto> laborDtos)
    {
        var incomingIds = laborDtos.Where(l => l.Id.HasValue).Select(l => l.Id!.Value).ToHashSet();
        var toRemove = activity.Labor.Where(l => !incomingIds.Contains(l.Id)).ToList();
        foreach (var labor in toRemove)
        {
            activity.Labor.Remove(labor);
        }

        foreach (var dto in laborDtos)
        {
            var labor = dto.Id.HasValue
                ? activity.Labor.First(l => l.Id == dto.Id.Value)
                : NewLabor(activity);

            labor.WorkerType = dto.WorkerType;
            labor.HourlyRate = dto.HourlyRate;
            labor.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static BudgetActivityLabor NewLabor(BudgetActivity activity)
    {
        var labor = new BudgetActivityLabor { CreatedAt = DateTime.UtcNow };
        activity.Labor.Add(labor);
        return labor;
    }

    private static BudgetResponseDto MapToDto(Budget budget) => new()
    {
        Id = budget.Id,
        CustomerId = budget.CustomerId,
        BranchId = budget.BranchId,
        Name = budget.Name,
        Status = budget.Status,
        UtilityPercentage = budget.UtilityPercentage,
        IndirectCostsTotal = budget.IndirectCostsTotal,
        TotalBudget = budget.TotalBudget,
        CreatedByUserId = budget.CreatedByUserId,
        Chapters = budget.Chapters
            .OrderBy(c => c.Order)
            .Select(c => new BudgetChapterResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                Order = c.Order,
                EstimatedWeeks = c.EstimatedWeeks,
                TotalChapter = c.TotalChapter,
                Activities = c.Activities.Select(a => new BudgetActivityResponseDto
                {
                    Id = a.Id,
                    Description = a.Description,
                    MaterialQuantity = a.MaterialQuantity,
                    MaterialCost = a.MaterialCost,
                    LaborCost = a.LaborCost,
                    EquipmentCost = a.EquipmentCost,
                    TotalActivity = a.TotalActivity,
                    Materials = a.Materials.Select(m => new BudgetActivityMaterialResponseDto
                    {
                        Id = m.Id,
                        MaterialId = m.MaterialId,
                        UnitPrice = m.UnitPrice
                    }).ToList(),
                    Equipment = a.Equipment.Select(e => new BudgetActivityEquipmentResponseDto
                    {
                        Id = e.Id,
                        EquipmentName = e.EquipmentName,
                        UnitPrice = e.UnitPrice
                    }).ToList(),
                    Labor = a.Labor.Select(l => new BudgetActivityLaborResponseDto
                    {
                        Id = l.Id,
                        WorkerType = l.WorkerType,
                        HourlyRate = l.HourlyRate
                    }).ToList()
                }).ToList()
            }).ToList()
    };
}
