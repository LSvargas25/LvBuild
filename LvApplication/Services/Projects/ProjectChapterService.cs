using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Projects;
using LvApplication.Services.Incidents;
using LvApplication.Services.Inventory;
using LvApplication.Services.Payroll;
using LvDomain.Entities.Projects;

namespace LvApplication.Services.Projects;

public class ProjectChapterService : IProjectChapterService
{
    private readonly IProjectChapterRepository _projectChapterRepository;
    private readonly IMaterialTicketRepository _ticketRepository;
    private readonly IPayrollRepository _payrollRepository;
    private readonly IIncidentRepository _incidentRepository;
    private readonly IProjectRepository _projectRepository;

    public ProjectChapterService(
        IProjectChapterRepository projectChapterRepository,
        IMaterialTicketRepository ticketRepository,
        IPayrollRepository payrollRepository,
        IIncidentRepository incidentRepository,
        IProjectRepository projectRepository
    )
    {
        _projectChapterRepository = projectChapterRepository;
        _ticketRepository = ticketRepository;
        _payrollRepository = payrollRepository;
        _incidentRepository = incidentRepository;
        _projectRepository = projectRepository;
    }

    public async Task<ProjectChapterDto> UpdateAssignedSoldTotalAsync(
        int projectId,
        int chapterId,
        decimal assignedSoldTotal
    )
    {
        var projectChapter =
            await _projectChapterRepository.GetByProjectAndChapterAsync(projectId, chapterId)
            ?? throw new NotFoundException(
                $"No hay un ProjectChapter para el proyecto {projectId} y el capítulo {chapterId}."
            );

        projectChapter.AssignedSoldTotal = assignedSoldTotal;
        projectChapter.ChapterProfit = assignedSoldTotal - projectChapter.ActualCostTotal;
        projectChapter.UpdatedAt = DateTime.UtcNow;

        await _projectChapterRepository.UpdateAsync(projectChapter);
        await SyncProjectProfitAsync(projectId);

        return MapToDto(projectChapter);
    }

    public async Task RecalculateActualCostAsync(int projectId, int chapterId)
    {
        var projectChapter =
            await _projectChapterRepository.GetByProjectAndChapterAsync(projectId, chapterId)
            ?? throw new NotFoundException(
                $"No hay un ProjectChapter para el proyecto {projectId} y el capítulo {chapterId}."
            );

        var ticketsTotal = await _ticketRepository.SumAppliedTotalByChapterAsync(
            projectId,
            chapterId
        );
        var payrollTotal = await _payrollRepository.SumPaidTotalByChapterAsync(
            projectId,
            chapterId
        );
        var (incidentCount, incidentsTotal) =
            await _incidentRepository.GetApprovedSummaryByChapterAsync(projectId, chapterId);

        // NO se incluye SiteLog.TotalMaterials aquí: ese costo ya queda contabilizado a través
        // de MaterialTicket.ApplyAsync — sumarlo de nuevo sería doble conteo, mismo criterio ya
        // usado para Project.CurrentDirectExpenses en fases anteriores.
        projectChapter.ActualCostTotal = ticketsTotal + payrollTotal + incidentsTotal;
        projectChapter.ChapterProfit =
            projectChapter.AssignedSoldTotal - projectChapter.ActualCostTotal;
        projectChapter.IncidentCount = incidentCount;
        projectChapter.IncidentPercentage =
            projectChapter.ActualCostTotal == 0
                ? null
                : incidentsTotal / projectChapter.ActualCostTotal * 100m;
        projectChapter.UpdatedAt = DateTime.UtcNow;

        await _projectChapterRepository.UpdateAsync(projectChapter);
        await SyncProjectProfitAsync(projectId);
    }

    /// <summary>
    /// Project.CurrentProfit is the sum of the chapters' profit (assigned sold total minus actual
    /// cost), the same figure the finance view totals, so both always agree.
    /// </summary>
    public async Task SyncProjectProfitAsync(int projectId)
    {
        var project =
            await _projectRepository.GetByIdAsync(projectId)
            ?? throw new NotFoundException($"Proyecto {projectId} no encontrado.");
        var chapters = await _projectChapterRepository.GetByProjectAsync(projectId);

        project.CurrentProfit = chapters.Sum(c => c.ChapterProfit);
        project.UpdatedAt = DateTime.UtcNow;
        await _projectRepository.UpdateAsync(project);
    }

    public async Task<List<ProjectChapterDto>> GetByProjectAsync(int projectId)
    {
        var chapters = await _projectChapterRepository.GetByProjectAsync(projectId);
        return chapters.Select(MapToDto).ToList();
    }

    private static ProjectChapterDto MapToDto(ProjectChapter chapter) =>
        new()
        {
            Id = chapter.Id,
            ProjectId = chapter.ProjectId,
            ChapterId = chapter.ChapterId,
            AssignedSoldTotal = chapter.AssignedSoldTotal,
            ActualCostTotal = chapter.ActualCostTotal,
            ChapterProfit = chapter.ChapterProfit,
            IncidentCount = chapter.IncidentCount,
            IncidentPercentage = chapter.IncidentPercentage,
        };
}
