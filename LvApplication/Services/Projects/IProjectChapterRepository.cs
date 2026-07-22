using LvDomain.Entities.Projects;

namespace LvApplication.Services.Projects;

public interface IProjectChapterRepository
{
    Task<ProjectChapter?> GetByIdAsync(int id);
    Task<ProjectChapter?> GetByProjectAndChapterAsync(int projectId, int chapterId);
    Task<List<ProjectChapter>> GetByProjectAsync(int projectId);
    Task AddAsync(ProjectChapter chapter);
    Task UpdateAsync(ProjectChapter chapter);
}
