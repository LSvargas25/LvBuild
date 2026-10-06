using LvApplication.Common;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Progress;
using LvApplication.Services.Offers;
using LvApplication.Services.Projects;
using LvApplication.Services.SiteLogs;
using LvDomain.Entities.Progress;

namespace LvApplication.Services.Progress;

public class ProjectProgressService : IProjectProgressService
{
    private readonly IProjectProgressRepository _progressRepository;
    private readonly ISiteLogRepository _siteLogRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IOfferRepository _offerRepository;

    public ProjectProgressService(
        IProjectProgressRepository progressRepository,
        ISiteLogRepository siteLogRepository,
        IProjectRepository projectRepository,
        IOfferRepository offerRepository
    )
    {
        _progressRepository = progressRepository;
        _siteLogRepository = siteLogRepository;
        _projectRepository = projectRepository;
        _offerRepository = offerRepository;
    }

    public async Task<ProjectProgressDto> CalculateAndRecordAsync(int siteLogId)
    {
        var siteLog =
            await _siteLogRepository.GetByIdAsync(siteLogId)
            ?? throw new NotFoundException($"No se encontró la bitácora {siteLogId}.");

        var project =
            await _projectRepository.GetByIdAsync(siteLog.ProjectId)
            ?? throw new NotFoundException($"No se encontró el proyecto {siteLog.ProjectId}.");

        var offer =
            await _offerRepository.GetByIdAsync(project.OfferId)
            ?? throw new NotFoundException($"No se encontró la oferta {project.OfferId}.");

        // SUPUESTO (la sección 13 de la especificación no trae la fórmula exacta):
        // % de avance = semanas transcurridas entre el inicio del proyecto y el fin de
        // semana de esta bitácora, sobre la duración estimada de la oferta, con tope en 100%.
        var elapsedWeeks = (decimal)(siteLog.WeekEnd - project.StartDate).TotalDays / 7m;
        var progressPercentage =
            offer.EstimatedDurationWeeks > 0
                ? Math.Min(100m, elapsedWeeks / offer.EstimatedDurationWeeks * 100m)
                : 100m;

        var progress = new ProjectProgress
        {
            ProjectId = project.Id,
            SiteLogId = siteLog.Id,
            ProgressPercentage = progressPercentage,
            CalculatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };

        await _progressRepository.AddAsync(progress);

        return MapToDto(progress);
    }

    public async Task<PagedResult<ProjectProgressDto>> GetHistoryByProjectAsync(
        int projectId,
        int pageNumber,
        int pageSize
    )
    {
        var (items, totalCount) = await _progressRepository.GetPagedByProjectAsync(
            projectId,
            pageNumber,
            pageSize
        );

        return new PagedResult<ProjectProgressDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
    }

    private static ProjectProgressDto MapToDto(ProjectProgress progress) =>
        new()
        {
            Id = progress.Id,
            ProjectId = progress.ProjectId,
            SiteLogId = progress.SiteLogId,
            ProgressPercentage = progress.ProgressPercentage,
            CalculatedAt = progress.CalculatedAt,
        };
}
