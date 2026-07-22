using FluentValidation;
using LvApplication.Common;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Projects;
using LvApplication.Services.Branches;
using LvApplication.Services.Budgets;
using LvApplication.Services.Offers;
using LvApplication.Services.Workers;
using LvDomain.Entities.Projects;
using LvDomain.Enums;

namespace LvApplication.Services.Projects;

public class ProjectService : IProjectService
{
    private const string GeneralManagerRole = "GeneralManager";

    private readonly IProjectRepository _projectRepository;
    private readonly IOfferRepository _offerRepository;
    private readonly IBudgetRepository _budgetRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IWorkerRepository _workerRepository;
    private readonly IProjectChapterRepository _projectChapterRepository;
    private readonly IValidator<CreateProjectDto> _createValidator;
    private readonly IValidator<UpdateEndDateDto> _updateEndDateValidator;
    private readonly IValidator<AssignWorkerDto> _assignWorkerValidator;

    public ProjectService(
        IProjectRepository projectRepository,
        IOfferRepository offerRepository,
        IBudgetRepository budgetRepository,
        IBranchRepository branchRepository,
        IWorkerRepository workerRepository,
        IProjectChapterRepository projectChapterRepository,
        IValidator<CreateProjectDto> createValidator,
        IValidator<UpdateEndDateDto> updateEndDateValidator,
        IValidator<AssignWorkerDto> assignWorkerValidator)
    {
        _projectRepository = projectRepository;
        _offerRepository = offerRepository;
        _budgetRepository = budgetRepository;
        _branchRepository = branchRepository;
        _workerRepository = workerRepository;
        _projectChapterRepository = projectChapterRepository;
        _createValidator = createValidator;
        _updateEndDateValidator = updateEndDateValidator;
        _assignWorkerValidator = assignWorkerValidator;
    }

    public async Task<ProjectDto> CreateProjectAsync(CreateProjectDto request, int createdByUserId)
    {
        await _createValidator.ValidateAndThrowAppExceptionAsync(request);

        var offer = await _offerRepository.GetByIdAsync(request.OfferId)
            ?? throw new NotFoundException($"Offer {request.OfferId} not found.");

        if (offer.Status != OfferStatus.ClientAccepted)
        {
            throw new ValidationAppException("Solo se puede crear un proyecto a partir de una oferta Aceptada por el Cliente.");
        }

        var budget = await _budgetRepository.GetByIdAsync(offer.BudgetId)
            ?? throw new NotFoundException($"Budget {offer.BudgetId} not found.");

        if (budget.Status != BudgetStatus.ClientApproved)
        {
            throw new ValidationAppException("El presupuesto asociado a la oferta debe estar en estado Aprobado por el Cliente.");
        }

        var existingProject = await _projectRepository.GetByOfferIdAsync(request.OfferId);
        if (existingProject is not null)
        {
            throw new ValidationAppException($"La oferta {request.OfferId} ya tiene un proyecto creado (Id {existingProject.Id}).");
        }

        var branch = await _branchRepository.GetByIdAsync(request.BranchId)
            ?? throw new NotFoundException($"Branch {request.BranchId} not found.");

        if (branch.BranchType != BranchType.Office)
        {
            throw new ValidationAppException("El proyecto solo puede crearse en una sucursal de tipo Oficina.");
        }

        var project = new Project
        {
            OfferId = offer.Id,
            BudgetId = offer.BudgetId,
            CustomerId = offer.CustomerId,
            BranchId = branch.Id,
            ProjectType = offer.OfferType == OfferType.Turnkey ? ProjectType.TurnKey : ProjectType.Percentage,
            StartDate = request.StartDate,
            EndDate = offer.EstimatedDeliveryDate,
            WeeksCounter = 0,
            TotalWorkedHours = 0,
            WorkersUsedCount = 0,
            MaterialsUsedCount = 0,
            CurrentDirectExpenses = 0,
            PendingExpenses = 0,
            CurrentProfit = 0,
            Status = ProjectStatus.Active,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _projectRepository.AddAsync(project);

        // SUPUESTO (sección 16, confirmado por el cliente 2026-07-21): el reparto proporcional
        // del precio total vendido solo aplica a proyectos Llave en Mano (TurnKey), donde sí
        // existe un precio total fijo (Offer.TotalProjectPrice) que repartir entre capítulos
        // según su peso en el presupuesto original. En Porcentaje no hay precio total fijo —
        // AssignedSoldTotal queda en 0 y se asigna manualmente vía PUT .../assigned-sold-total.
        foreach (var chapter in budget.Chapters)
        {
            var assignedSoldTotal = project.ProjectType == ProjectType.TurnKey && budget.TotalBudget > 0
                ? (chapter.TotalChapter / budget.TotalBudget) * (offer.TotalProjectPrice ?? 0)
                : 0;

            await _projectChapterRepository.AddAsync(new LvDomain.Entities.Projects.ProjectChapter
            {
                ProjectId = project.Id,
                ChapterId = chapter.Id,
                AssignedSoldTotal = assignedSoldTotal,
                ActualCostTotal = 0,
                ChapterProfit = assignedSoldTotal,
                IncidentCount = 0,
                CreatedAt = DateTime.UtcNow
            });
        }

        return MapToDto(project);
    }

    public async Task<ProjectDto> UpdateEndDateAsync(int id, UpdateEndDateDto request, int actingUserId)
    {
        await _updateEndDateValidator.ValidateAndThrowAppExceptionAsync(request);

        var project = await _projectRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Project {id} not found.");

        var previousDate = project.EndDate;

        project.EndDate = request.NewEndDate;
        project.UpdatedAt = DateTime.UtcNow;

        project.EndDateHistory.Add(new ProjectEndDateHistory
        {
            PreviousDate = previousDate,
            NewDate = request.NewEndDate,
            Reason = request.Reason,
            UserId = actingUserId,
            ChangedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });

        await _projectRepository.UpdateAsync(project);

        return MapToDto(project);
    }

    public async Task<ProjectDto> AssignWorkerAsync(int id, AssignWorkerDto request, int actingUserId)
    {
        await _assignWorkerValidator.ValidateAndThrowAppExceptionAsync(request);

        var project = await _projectRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Project {id} not found.");

        var worker = await _workerRepository.GetByIdAsync(request.WorkerId)
            ?? throw new NotFoundException($"Worker {request.WorkerId} not found.");

        if (worker.Status != ActiveStatus.Active)
        {
            throw new ValidationAppException("Solo se pueden asignar trabajadores activos.");
        }

        var existingAssignment = project.Workers.FirstOrDefault(pw => pw.WorkerId == request.WorkerId);

        if (existingAssignment is not null)
        {
            existingAssignment.IsActive = true;
            existingAssignment.AssignedAt = DateTime.UtcNow;
            existingAssignment.AssignedByUserId = actingUserId;
            existingAssignment.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            project.Workers.Add(new ProjectWorker
            {
                WorkerId = worker.Id,
                AssignedAt = DateTime.UtcNow,
                AssignedByUserId = actingUserId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _projectRepository.UpdateAsync(project);

        return MapToDto(project);
    }

    public async Task<ProjectDto> UnassignWorkerAsync(int id, int workerId)
    {
        var project = await _projectRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Project {id} not found.");

        var assignment = project.Workers.FirstOrDefault(pw => pw.WorkerId == workerId && pw.IsActive)
            ?? throw new NotFoundException($"No hay una asignación activa del trabajador {workerId} en este proyecto.");

        assignment.IsActive = false;
        assignment.UpdatedAt = DateTime.UtcNow;

        await _projectRepository.UpdateAsync(project);

        return MapToDto(project);
    }

    public async Task DeleteAsync(int id)
    {
        var project = await _projectRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Project {id} not found.");

        var hasActivity = project.Workers.Count > 0
            || project.WeeksCounter != 0
            || project.TotalWorkedHours != 0
            || project.CurrentDirectExpenses != 0
            || project.PendingExpenses != 0;

        if (hasActivity)
        {
            throw new ValidationAppException("Este proyecto ya tiene actividad registrada (trabajadores asignados, semanas o gastos) y no puede eliminarse.");
        }

        await _projectRepository.DeleteAsync(project);
    }

    public async Task<ProjectDto> GetByIdAsync(int id)
    {
        var project = await _projectRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Project {id} not found.");
        return MapToDto(project);
    }

    public async Task<PagedResult<ProjectDto>> GetAllAsync(int pageNumber, int pageSize)
    {
        var (items, totalCount) = await _projectRepository.GetPagedAsync(pageNumber, pageSize);

        return new PagedResult<ProjectDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<List<ProjectEndDateHistoryDto>> GetEndDateHistoryAsync(int projectId)
    {
        var history = await _projectRepository.GetEndDateHistoryAsync(projectId);

        return history
            .OrderBy(h => h.ChangedAt)
            .Select(h => new ProjectEndDateHistoryDto
            {
                Id = h.Id,
                ProjectId = h.ProjectId,
                PreviousDate = h.PreviousDate,
                NewDate = h.NewDate,
                Reason = h.Reason,
                UserId = h.UserId,
                ChangedAt = h.ChangedAt
            })
            .ToList();
    }

    public async Task IncrementWeekCounterAsync(int projectId)
    {
        var project = await _projectRepository.GetByIdAsync(projectId) ?? throw new NotFoundException($"Project {projectId} not found.");

        project.WeeksCounter += 1;
        project.UpdatedAt = DateTime.UtcNow;

        await _projectRepository.UpdateAsync(project);
    }

    public async Task DecrementWeekCounterAsync(int projectId, IEnumerable<string> actingUserRoles)
    {
        if (!actingUserRoles.Contains(GeneralManagerRole))
        {
            throw new ForbiddenException("Solo el Gerente General puede restar semanas al contador del proyecto.");
        }

        var project = await _projectRepository.GetByIdAsync(projectId) ?? throw new NotFoundException($"Project {projectId} not found.");

        if (project.WeeksCounter > 0)
        {
            project.WeeksCounter -= 1;
        }

        project.UpdatedAt = DateTime.UtcNow;

        await _projectRepository.UpdateAsync(project);
    }

    private static ProjectDto MapToDto(Project project) => new()
    {
        Id = project.Id,
        OfferId = project.OfferId,
        BudgetId = project.BudgetId,
        CustomerId = project.CustomerId,
        BranchId = project.BranchId,
        ProjectType = project.ProjectType,
        StartDate = project.StartDate,
        EndDate = project.EndDate,
        WeeksCounter = project.WeeksCounter,
        TotalWorkedHours = project.TotalWorkedHours,
        WorkersUsedCount = project.Workers.Count(w => w.IsActive),
        MaterialsUsedCount = project.MaterialsUsedCount,
        CurrentDirectExpenses = project.CurrentDirectExpenses,
        PendingExpenses = project.PendingExpenses,
        CurrentProfit = project.CurrentProfit,
        Status = project.Status,
        CreatedByUserId = project.CreatedByUserId,
        Workers = project.Workers
            .Where(w => w.IsActive)
            .Select(w => new ProjectWorkerDto
            {
                Id = w.Id,
                WorkerId = w.WorkerId,
                AssignedAt = w.AssignedAt,
                AssignedByUserId = w.AssignedByUserId,
                IsActive = w.IsActive
            }).ToList()
    };
}
