using LvApplication.DTOs.Projects;

namespace LvApplication.Services.Projects;

public interface IProjectChapterService
{
    Task<ProjectChapterDto> UpdateAssignedSoldTotalAsync(
        int projectId,
        int chapterId,
        decimal assignedSoldTotal
    );
    Task RecalculateActualCostAsync(int projectId, int chapterId);
    Task<List<ProjectChapterDto>> GetByProjectAsync(int projectId);
}
