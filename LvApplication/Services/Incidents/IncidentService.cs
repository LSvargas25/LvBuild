using FluentValidation;
using LvApplication.Common;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Incidents;
using LvApplication.Services.Inventory;
using LvApplication.Services.Projects;
using LvApplication.Services.Workers;
using LvDomain.Entities.Incidents;
using LvDomain.Enums;

namespace LvApplication.Services.Incidents;

public class IncidentService : IIncidentService
{
    private readonly IIncidentRepository _incidentRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectInventoryItemRepository _inventoryRepository;
    private readonly IWorkerRepository _workerRepository;
    private readonly IValidator<CreateIncidentDto> _createValidator;
    private readonly IValidator<UpdateIncidentDto> _updateValidator;

    public IncidentService(
        IIncidentRepository incidentRepository,
        IProjectRepository projectRepository,
        IProjectInventoryItemRepository inventoryRepository,
        IWorkerRepository workerRepository,
        IValidator<CreateIncidentDto> createValidator,
        IValidator<UpdateIncidentDto> updateValidator)
    {
        _incidentRepository = incidentRepository;
        _projectRepository = projectRepository;
        _inventoryRepository = inventoryRepository;
        _workerRepository = workerRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IncidentDto> CreateAsync(CreateIncidentDto request, int createdByUserId)
    {
        await _createValidator.ValidateAndThrowAppExceptionAsync(request);

        var project = await _projectRepository.GetByIdAsync(request.ProjectId)
            ?? throw new NotFoundException($"Project {request.ProjectId} not found.");

        var incident = new Incident
        {
            ProjectId = project.Id,
            Date = request.Date,
            Description = request.Description,
            Status = IncidentStatus.Draft,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        var materialsCost = await SyncMaterialsAsync(project.Id, incident, request.Materials);
        var workersCost = await SyncWorkersAsync(incident, request.Workers);
        incident.TotalCost = materialsCost + workersCost;

        await _incidentRepository.AddAsync(incident);

        project.PendingExpenses += incident.TotalCost;
        project.UpdatedAt = DateTime.UtcNow;
        await _projectRepository.UpdateAsync(project);

        return MapToDto(incident);
    }

    public async Task<IncidentDto> UpdateAsync(int id, UpdateIncidentDto request)
    {
        await _updateValidator.ValidateAndThrowAppExceptionAsync(request);

        var incident = await _incidentRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Incident {id} not found.");

        if (incident.Status != IncidentStatus.Draft)
        {
            throw new ValidationAppException("Solo se puede editar un imprevisto en estado Borrador.");
        }

        var previousTotalCost = incident.TotalCost;

        incident.Date = request.Date;
        incident.Description = request.Description;
        incident.UpdatedAt = DateTime.UtcNow;

        var materialsCost = await SyncMaterialsAsync(incident.ProjectId, incident, request.Materials);
        var workersCost = await SyncWorkersAsync(incident, request.Workers);
        incident.TotalCost = materialsCost + workersCost;

        await _incidentRepository.UpdateAsync(incident);

        var project = await _projectRepository.GetByIdAsync(incident.ProjectId)
            ?? throw new NotFoundException($"Project {incident.ProjectId} not found.");
        project.PendingExpenses += incident.TotalCost - previousTotalCost;
        project.UpdatedAt = DateTime.UtcNow;
        await _projectRepository.UpdateAsync(project);

        return MapToDto(incident);
    }

    public async Task<IncidentDto> ApproveAsync(int id, int approvedByUserId)
    {
        var incident = await _incidentRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Incident {id} not found.");

        if (incident.Status != IncidentStatus.Draft)
        {
            throw new ValidationAppException("Solo se puede aprobar un imprevisto en estado Borrador.");
        }

        incident.Status = IncidentStatus.Approved;
        incident.ApprovedByUserId = approvedByUserId;
        incident.UpdatedAt = DateTime.UtcNow;
        await _incidentRepository.UpdateAsync(incident);

        var project = await _projectRepository.GetByIdAsync(incident.ProjectId)
            ?? throw new NotFoundException($"Project {incident.ProjectId} not found.");
        project.PendingExpenses -= incident.TotalCost;
        project.CurrentDirectExpenses += incident.TotalCost;
        project.UpdatedAt = DateTime.UtcNow;
        await _projectRepository.UpdateAsync(project);

        return MapToDto(incident);
    }

    public async Task DeleteAsync(int id)
    {
        var incident = await _incidentRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Incident {id} not found.");

        if (incident.Status != IncidentStatus.Draft)
        {
            throw new ValidationAppException("Solo se puede eliminar un imprevisto en estado Borrador.");
        }

        var project = await _projectRepository.GetByIdAsync(incident.ProjectId)
            ?? throw new NotFoundException($"Project {incident.ProjectId} not found.");
        project.PendingExpenses -= incident.TotalCost;
        project.UpdatedAt = DateTime.UtcNow;
        await _projectRepository.UpdateAsync(project);

        await _incidentRepository.DeleteAsync(incident);
    }

    public async Task<IncidentDto> GetByIdAsync(int id)
    {
        var incident = await _incidentRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Incident {id} not found.");
        return MapToDto(incident);
    }

    public async Task<PagedResult<IncidentDto>> GetAllAsync(int pageNumber, int pageSize)
    {
        var (items, totalCount) = await _incidentRepository.GetPagedAsync(pageNumber, pageSize);

        return new PagedResult<IncidentDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<IncidentDto>> GetAllByProjectAsync(int projectId, int pageNumber, int pageSize)
    {
        var (items, totalCount) = await _incidentRepository.GetPagedByProjectAsync(projectId, pageNumber, pageSize);

        return new PagedResult<IncidentDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    // SUPUESTO (la sección 14 de la especificación no da la base de costo): el costo de
    // cada material se valora al ReferenceUnitCost vigente del inventario del proyecto —
    // no se descuenta CurrentQuantity, solo se usa como referencia de costo unitario.
    private async Task<decimal> SyncMaterialsAsync(int projectId, Incident incident, List<IncidentMaterialDto> materialDtos)
    {
        var incomingMaterialIds = materialDtos.Select(m => m.MaterialId).ToHashSet();
        var toRemove = incident.Materials.Where(m => !incomingMaterialIds.Contains(m.MaterialId)).ToList();
        foreach (var material in toRemove)
        {
            incident.Materials.Remove(material);
        }

        decimal total = 0;

        foreach (var dto in materialDtos)
        {
            var inventoryItem = await _inventoryRepository.GetByProjectAndMaterialAsync(projectId, dto.MaterialId)
                ?? throw new NotFoundException($"No hay inventario del material {dto.MaterialId} en el proyecto {projectId}.");

            var material = incident.Materials.FirstOrDefault(m => m.MaterialId == dto.MaterialId);
            if (material is null)
            {
                material = new IncidentMaterial { MaterialId = dto.MaterialId, CreatedAt = DateTime.UtcNow };
                incident.Materials.Add(material);
            }

            material.Quantity = dto.Quantity;
            material.UpdatedAt = DateTime.UtcNow;

            total += dto.Quantity * inventoryItem.ReferenceUnitCost;
        }

        return total;
    }

    private async Task<decimal> SyncWorkersAsync(Incident incident, List<IncidentWorkerDto> workerDtos)
    {
        var incomingWorkerIds = workerDtos.Select(w => w.WorkerId).ToHashSet();
        var toRemove = incident.Workers.Where(w => !incomingWorkerIds.Contains(w.WorkerId)).ToList();
        foreach (var worker in toRemove)
        {
            incident.Workers.Remove(worker);
        }

        decimal total = 0;

        foreach (var dto in workerDtos)
        {
            var workerEntity = await _workerRepository.GetByIdAsync(dto.WorkerId)
                ?? throw new NotFoundException($"Worker {dto.WorkerId} not found.");

            var incidentWorker = incident.Workers.FirstOrDefault(w => w.WorkerId == dto.WorkerId);
            if (incidentWorker is null)
            {
                incidentWorker = new IncidentWorker { WorkerId = dto.WorkerId, CreatedAt = DateTime.UtcNow };
                incident.Workers.Add(incidentWorker);
            }

            incidentWorker.HoursUsed = dto.HoursUsed;
            incidentWorker.UpdatedAt = DateTime.UtcNow;

            total += dto.HoursUsed * workerEntity.HourlyRate;
        }

        return total;
    }

    private static IncidentDto MapToDto(Incident incident) => new()
    {
        Id = incident.Id,
        ProjectId = incident.ProjectId,
        Date = incident.Date,
        Description = incident.Description,
        Status = incident.Status,
        TotalCost = incident.TotalCost,
        CreatedByUserId = incident.CreatedByUserId,
        ApprovedByUserId = incident.ApprovedByUserId,
        Materials = incident.Materials.Select(m => new IncidentMaterialResponseDto
        {
            Id = m.Id,
            MaterialId = m.MaterialId,
            Quantity = m.Quantity
        }).ToList(),
        Workers = incident.Workers.Select(w => new IncidentWorkerResponseDto
        {
            Id = w.Id,
            WorkerId = w.WorkerId,
            HoursUsed = w.HoursUsed
        }).ToList()
    };
}
