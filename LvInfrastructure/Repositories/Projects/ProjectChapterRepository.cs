using LvApplication.Services.Projects;
using LvDomain.Entities.Projects;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LvInfrastructure.Repositories.Projects;

public class ProjectChapterRepository : IProjectChapterRepository
{
    private readonly AppDbContext _context;

    public ProjectChapterRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<ProjectChapter?> GetByIdAsync(int id) =>
        _context.ProjectChapters.FirstOrDefaultAsync(c => c.Id == id);

    public Task<ProjectChapter?> GetByProjectAndChapterAsync(int projectId, int chapterId) =>
        _context.ProjectChapters.FirstOrDefaultAsync(c => c.ProjectId == projectId && c.ChapterId == chapterId);

    public Task<List<ProjectChapter>> GetByProjectAsync(int projectId) =>
        _context.ProjectChapters.Where(c => c.ProjectId == projectId).OrderBy(c => c.ChapterId).ToListAsync();

    public async Task AddAsync(ProjectChapter chapter)
    {
        _context.ProjectChapters.Add(chapter);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ProjectChapter chapter)
    {
        _context.ProjectChapters.Update(chapter);
        await _context.SaveChangesAsync();
    }
}
