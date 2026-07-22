# ProjectChapter Retrofit + Finance View Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Retrofit chapter-level cost tagging (`ChapterId`, nullable) onto `MaterialTicket`, `Payroll`, `SiteLog`, and `Incident`; add a `ProjectChapter` aggregate that is auto-created per `BudgetChapter` when a `Project` is created and recalculated whenever a tagged ticket/payroll/incident completes; and add a read-only, real-time-aggregated Finance view — all without changing any existing method signature that tests already call (only additive constructor parameters absorbed entirely inside `ServiceFactory`/`ServiceCollectionExtensions`, and additive nullable DTO/entity fields that default to `null`/unset in every pre-existing test).

**Architecture:** `ProjectChapter` lives in `LvDomain.Entities.Projects` next to `Project` (per explicit instruction) with its own repository/service under the existing `Projects` module folder. Tagging validation ("chapter must belong to this project's budget") is a **service-level** check (matches the user's explicit instruction: "valida esto en cada Create/Update de los 4 services"), not a validator-level check — each of the 4 services gets a small private `ValidateChapterAsync` helper and a new `IBudgetRepository` dependency. `IProjectChapterService.RecalculateActualCostAsync` is called by `MaterialTicketService.ApplyAsync`, `PayrollService.MarkAsPaidAsync`, `IncidentService.ApproveAsync` (never by `SiteLogService` — its cost is already counted through `MaterialTicket.ApplyAsync`, so counting it again would double-count, exactly like how `SiteLog.TotalMaterials` never touches `Project.CurrentDirectExpenses` directly). The Finance view is a new `ProjectFinanceService` composing two new read-only repository methods (`GetAppliedInRangeAsync`, `GetInRangeAsync`) — no new movement-ledger table, per the spec's explicit permission ("no necesariamente tabla física nueva").

**Tech Stack:** .NET 8, EF Core (`UseInMemoryDatabase` in tests), FluentValidation, xUnit + FluentAssertions.

## Global Constraints

- **Backward compatibility is the top priority of this plan.** Every new entity field is a nullable `int? ChapterId`; every new constructor parameter is appended in a way that only `ServiceFactory.cs`/`ServiceCollectionExtensions.cs` need to change — no test file that doesn't explicitly test chapter behavior should need a single line changed. Run the full suite after every task; if anything outside the intended file breaks, stop and investigate before continuing.
- Tagging granularity is **whole-record** (confirmed by the client): one `ChapterId` per `MaterialTicket`/`Payroll`/`SiteLog`/`Incident`, never per line item.
- Chapter validation ("must belong to the project's budget") lives in the **service** layer of each of the 4 retrofitted services, not in FluentValidation — the validators only check `ChapterId > 0` when provided.
- `RecalculateActualCostAsync` explicitly excludes `SiteLog.TotalMaterials` — that cost is already counted via `MaterialTicket.ApplyAsync`. Counting it again would double-count, mirroring the exact reasoning already applied to `Project.CurrentDirectExpenses` in earlier phases.
- Two documented assumptions (spec doesn't give exact numbers) go as code comments at their exact point of use:
  1. `ProjectChapter.AssignedSoldTotal` proportional split only applies to `ProjectType.TurnKey` (uses `Offer.TotalProjectPrice`); `Percentage` projects start at 0 and require the manual PUT endpoint.
  2. Finance period boundaries: `week` = 7-day window starting at the given `date` (same `WeekStart`→`WeekEnd` convention used everywhere else); `month` = calendar month containing `date`; `year` = calendar year containing `date`.
- **Do NOT generate an EF Core migration** — explicitly deferred by the user, even though this phase needs one (both the retrofit and the new `ProjectChapters` table).
- Money fields: `decimal(18,2)`. Percentage fields: `decimal(5,2)`.

---

## File Structure

| File | Responsibility |
|---|---|
| `LvDomain/Enums/FinancePeriod.cs` | `Week`, `Month`, `Year` |
| `LvDomain/Entities/Projects/ProjectChapter.cs` | New aggregate |
| `LvDomain/Entities/Inventory/MaterialTicket.cs` | Modify: add `ChapterId` |
| `LvDomain/Entities/Payroll/Payroll.cs` | Modify: add `ChapterId` |
| `LvDomain/Entities/SiteLogs/SiteLog.cs` | Modify: add `ChapterId` |
| `LvDomain/Entities/Incidents/Incident.cs` | Modify: add `ChapterId` |
| `LvApplication/DTOs/Projects/ProjectChapterDto.cs`, `UpdateAssignedSoldTotalDto.cs` | New DTOs |
| `LvApplication/DTOs/Finance/ProjectFinanceDto.cs`, `ProjectFinanceMaterialDto.cs` | New DTOs |
| `LvApplication/DTOs/Inventory/Create|Update|MaterialTicketDto.cs` | Modify: add `ChapterId` |
| `LvApplication/DTOs/Payroll/Create|Update|PayrollDto.cs` | Modify: add `ChapterId` |
| `LvApplication/DTOs/SiteLogs/Create|Update|SiteLogDto.cs` | Modify: add `ChapterId` |
| `LvApplication/DTOs/Incidents/Create|Update|IncidentDto.cs` | Modify: add `ChapterId` |
| `LvApplication/Validators/Projects/UpdateAssignedSoldTotalDtoValidator.cs` | New validator |
| `LvApplication/Validators/{Inventory,Payroll,SiteLogs,Incidents}/*Validator.cs` | Modify: add `ChapterId` rule |
| `LvApplication/Services/Projects/IProjectChapterRepository.cs`, `IProjectChapterService.cs`, `ProjectChapterService.cs` | New |
| `LvApplication/Services/Projects/ProjectService.cs` | Modify: auto-create `ProjectChapter`s |
| `LvApplication/Services/Inventory/{IMaterialTicketRepository,MaterialTicketService}.cs` | Modify: chapter validation + recalculation trigger + finance range query |
| `LvApplication/Services/Payroll/{IPayrollRepository,PayrollService}.cs` | Modify: same |
| `LvApplication/Services/SiteLogs/{ISiteLogRepository,SiteLogService}.cs` | Modify: chapter validation + finance range query (no recalculation trigger) |
| `LvApplication/Services/Incidents/{IIncidentRepository,IncidentService}.cs` | Modify: same as MaterialTicket/Payroll |
| `LvApplication/Services/Finance/IProjectFinanceService.cs`, `ProjectFinanceService.cs` | New |
| `LvInfrastructure/Persistence/Configurations/Projects/ProjectChapterConfiguration.cs` | New |
| `LvInfrastructure/Persistence/Configurations/{Inventory,Payroll,SiteLogs,Incidents}/*Configuration.cs` | Modify: add `ChapterId` FK |
| `LvInfrastructure/Repositories/Projects/ProjectChapterRepository.cs` | New |
| `LvInfrastructure/Repositories/{Inventory,Payroll,SiteLogs,Incidents}/*Repository.cs` | Modify: new query methods |
| `LvInfrastructure/Persistence/AppDbContext.cs` | Modify: add `DbSet<ProjectChapter>` |
| `LvApi/Controllers/Projects/ProjectChaptersController.cs` | New |
| `LvApi/Controllers/Finance/ProjectFinanceController.cs` | New |
| `LvApi/Extensions/ServiceCollectionExtensions.cs` | Modify: register everything |
| `LvTest/Common/ServiceFactory.cs` | Modify: absorb every new constructor parameter |
| `LvTest/Services/Projects/ProjectServiceTests.cs` | Modify: add chapter auto-creation tests |
| `LvTest/Services/Projects/ProjectChapterServiceTests.cs` | New |
| `LvTest/Services/Finance/ProjectFinanceServiceTests.cs` | New |

---

### Task 1: Enum + `ProjectChapter` entity + `ChapterId` retrofit on 4 entities

**Files:**
- Create: `LvDomain/Enums/FinancePeriod.cs`
- Create: `LvDomain/Entities/Projects/ProjectChapter.cs`
- Modify: `LvDomain/Entities/Inventory/MaterialTicket.cs`, `LvDomain/Entities/Payroll/Payroll.cs`, `LvDomain/Entities/SiteLogs/SiteLog.cs`, `LvDomain/Entities/Incidents/Incident.cs`

**Interfaces:**
- Consumes: `LvDomain.Entities.Budgets.BudgetChapter`, `LvDomain.Entities.Projects.Project` (pre-existing).
- Produces: `ProjectChapter { Id, ProjectId, Project, ChapterId, Chapter, AssignedSoldTotal, ActualCostTotal, ChapterProfit, IncidentCount, IncidentPercentage }`; `MaterialTicket.ChapterId`/`Chapter`, `Payroll.ChapterId`/`Chapter`, `SiteLog.ChapterId`/`Chapter`, `Incident.ChapterId`/`Chapter` (all `int?`/nullable nav) — every later task depends on these.

- [ ] **Step 1: Create `FinancePeriod.cs`**

```csharp
namespace LvDomain.Enums;

public enum FinancePeriod
{
    Week,
    Month,
    Year
}
```

- [ ] **Step 2: Create `ProjectChapter.cs`**

```csharp
using LvDomain.Common;
using LvDomain.Entities.Budgets;

namespace LvDomain.Entities.Projects;

public class ProjectChapter : BaseEntity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int ChapterId { get; set; }
    public BudgetChapter Chapter { get; set; } = null!;

    public decimal AssignedSoldTotal { get; set; }
    public decimal ActualCostTotal { get; set; }
    public decimal ChapterProfit { get; set; }
    public int IncidentCount { get; set; }
    public decimal? IncidentPercentage { get; set; }
}
```

- [ ] **Step 3: Add `ChapterId` to `MaterialTicket.cs`**

Add `using LvDomain.Entities.Budgets;` to the `using` block, then add after the `Status` property:

```csharp
    public int? ChapterId { get; set; }
    public BudgetChapter? Chapter { get; set; }
```

- [ ] **Step 4: Add `ChapterId` to `Payroll.cs`**

Add `using LvDomain.Entities.Budgets;` to the `using` block, then add after `PaidAt`:

```csharp

    public int? ChapterId { get; set; }
    public BudgetChapter? Chapter { get; set; }
```

- [ ] **Step 5: Add `ChapterId` to `SiteLog.cs`**

Add `using LvDomain.Entities.Budgets;` to the `using` block, then add after `ApprovedByUser`:

```csharp

    public int? ChapterId { get; set; }
    public BudgetChapter? Chapter { get; set; }
```

- [ ] **Step 6: Add `ChapterId` to `Incident.cs`**

Add `using LvDomain.Entities.Budgets;` to the `using` block, then add after `ApprovedByUser`:

```csharp

    public int? ChapterId { get; set; }
    public BudgetChapter? Chapter { get; set; }
```

- [ ] **Step 7: Build to verify it compiles**

Run: `dotnet build LvTest/LvDomain/LvDomain.csproj`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 8: Commit**

```bash
git add LvTest/LvDomain/Enums/FinancePeriod.cs LvTest/LvDomain/Entities/Projects/ProjectChapter.cs LvTest/LvDomain/Entities/Inventory/MaterialTicket.cs LvTest/LvDomain/Entities/Payroll/Payroll.cs LvTest/LvDomain/Entities/SiteLogs/SiteLog.cs LvTest/LvDomain/Entities/Incidents/Incident.cs
git commit -m "feat(chapters): add ProjectChapter entity and ChapterId retrofit"
```

---

### Task 2: EF Core configuration

**Files:**
- Create: `LvInfrastructure/Persistence/Configurations/Projects/ProjectChapterConfiguration.cs`
- Modify: `LvInfrastructure/Persistence/Configurations/Inventory/MaterialTicketConfiguration.cs`, `LvInfrastructure/Persistence/Configurations/Payroll/PayrollConfiguration.cs`, `LvInfrastructure/Persistence/Configurations/SiteLogs/SiteLogConfiguration.cs`, `LvInfrastructure/Persistence/Configurations/Incidents/IncidentConfiguration.cs`
- Modify: `LvInfrastructure/Persistence/AppDbContext.cs`

**Interfaces:**
- Consumes: entities from Task 1.
- Produces: `AppDbContext.ProjectChapters` `DbSet`; `ChapterId` FK columns on the 4 existing tables.

- [ ] **Step 1: Create `ProjectChapterConfiguration.cs`**

```csharp
using LvDomain.Entities.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Projects;

public class ProjectChapterConfiguration : IEntityTypeConfiguration<ProjectChapter>
{
    public void Configure(EntityTypeBuilder<ProjectChapter> builder)
    {
        builder.ToTable("ProjectChapters");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.AssignedSoldTotal).HasColumnType("decimal(18,2)");
        builder.Property(c => c.ActualCostTotal).HasColumnType("decimal(18,2)");
        builder.Property(c => c.ChapterProfit).HasColumnType("decimal(18,2)");
        builder.Property(c => c.IncidentPercentage).HasColumnType("decimal(5,2)");

        builder.HasOne(c => c.Project)
            .WithMany()
            .HasForeignKey(c => c.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Chapter)
            .WithMany()
            .HasForeignKey(c => c.ChapterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.ProjectId, c.ChapterId }).IsUnique();
    }
}
```

- [ ] **Step 2: Add the `ChapterId` FK to `MaterialTicketConfiguration.cs`**

Add before the closing `}` of `Configure`:

```csharp

        builder.HasOne(t => t.Chapter)
            .WithMany()
            .HasForeignKey(t => t.ChapterId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => t.ChapterId);
```

- [ ] **Step 3: Add the `ChapterId` FK to `PayrollConfiguration.cs`**

Add before the closing `}` of `Configure`:

```csharp

        builder.HasOne(p => p.Chapter)
            .WithMany()
            .HasForeignKey(p => p.ChapterId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.ChapterId);
```

- [ ] **Step 4: Add the `ChapterId` FK to `SiteLogConfiguration.cs`**

Add before the closing `}` of `Configure`:

```csharp

        builder.HasOne(s => s.Chapter)
            .WithMany()
            .HasForeignKey(s => s.ChapterId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(s => s.ChapterId);
```

- [ ] **Step 5: Add the `ChapterId` FK to `IncidentConfiguration.cs`**

Add before the closing `}` of `Configure`:

```csharp

        builder.HasOne(i => i.Chapter)
            .WithMany()
            .HasForeignKey(i => i.ChapterId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(i => i.ChapterId);
```

- [ ] **Step 6: Add `DbSet<ProjectChapter>` to `AppDbContext.cs`**

Add after `public DbSet<IncidentWorker> IncidentWorkers => Set<IncidentWorker>();`:

```csharp

    public DbSet<ProjectChapter> ProjectChapters => Set<ProjectChapter>();
```

(`ProjectChapter` resolves via the already-present `using LvDomain.Entities.Projects;`.)

- [ ] **Step 7: Build to verify it compiles**

Run: `dotnet build LvTest/LvInfrastructure/LvInfrastructure.csproj`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 8: Commit**

```bash
git add LvTest/LvInfrastructure/Persistence/Configurations/Projects/ProjectChapterConfiguration.cs LvTest/LvInfrastructure/Persistence/Configurations/Inventory/MaterialTicketConfiguration.cs LvTest/LvInfrastructure/Persistence/Configurations/Payroll/PayrollConfiguration.cs LvTest/LvInfrastructure/Persistence/Configurations/SiteLogs/SiteLogConfiguration.cs LvTest/LvInfrastructure/Persistence/Configurations/Incidents/IncidentConfiguration.cs LvTest/LvInfrastructure/Persistence/AppDbContext.cs
git commit -m "feat(chapters): add EF Core configuration for ProjectChapter and ChapterId FKs"
```

---

### Task 3: Repository retrofits (new `ProjectChapter` repo + new query methods on 4 existing repos)

**Files:**
- Create: `LvApplication/Services/Projects/IProjectChapterRepository.cs`, `LvInfrastructure/Repositories/Projects/ProjectChapterRepository.cs`
- Modify: `LvApplication/Services/Inventory/IMaterialTicketRepository.cs`, `LvInfrastructure/Repositories/Inventory/MaterialTicketRepository.cs`
- Modify: `LvApplication/Services/Payroll/IPayrollRepository.cs`, `LvInfrastructure/Repositories/Payroll/PayrollRepository.cs`
- Modify: `LvApplication/Services/SiteLogs/ISiteLogRepository.cs`, `LvInfrastructure/Repositories/SiteLogs/SiteLogRepository.cs`
- Modify: `LvApplication/Services/Incidents/IIncidentRepository.cs`, `LvInfrastructure/Repositories/Incidents/IncidentRepository.cs`

**Interfaces:**
- Produces: `IProjectChapterRepository { GetByIdAsync, GetByProjectAndChapterAsync, GetByProjectAsync, AddAsync, UpdateAsync }`; `IMaterialTicketRepository` gains `SumAppliedTotalByChapterAsync(int,int)` and `GetAppliedInRangeAsync(int,DateTime,DateTime)`; `IPayrollRepository` gains `SumPaidTotalByChapterAsync(int,int)`; `ISiteLogRepository` gains `GetInRangeAsync(int,DateTime,DateTime)`; `IIncidentRepository` gains `GetApprovedSummaryByChapterAsync(int,int) -> (int Count, decimal TotalCost)` — Tasks 6, 7, 8 depend on these exact signatures.

- [ ] **Step 1: Create `IProjectChapterRepository.cs`**

```csharp
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
```

- [ ] **Step 2: Create `ProjectChapterRepository.cs`**

```csharp
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
```

- [ ] **Step 3: Add the two new methods to `IMaterialTicketRepository.cs`**

Add before the closing `}`:

```csharp
    Task<decimal> SumAppliedTotalByChapterAsync(int projectId, int chapterId);
    Task<List<MaterialTicket>> GetAppliedInRangeAsync(int projectId, DateTime from, DateTime to);
```

- [ ] **Step 4: Implement both in `MaterialTicketRepository.cs`**

Add `using LvDomain.Enums;` to the `using` block, then add before the closing `}` of the class:

```csharp

    public Task<decimal> SumAppliedTotalByChapterAsync(int projectId, int chapterId) =>
        _context.MaterialTickets
            .Where(t => t.ProjectId == projectId && t.ChapterId == chapterId && t.Status == MaterialTicketStatus.Applied)
            .SumAsync(t => (decimal?)t.Total) is var sumTask
            ? sumTask.ContinueWith(t => t.Result ?? 0m)
            : Task.FromResult(0m);

    public Task<List<MaterialTicket>> GetAppliedInRangeAsync(int projectId, DateTime from, DateTime to) =>
        _context.MaterialTickets
            .Include(t => t.Supplier)
            .Where(t => t.ProjectId == projectId
                && t.Status == MaterialTicketStatus.Applied
                && t.CreatedAt >= from
                && t.CreatedAt < to.AddDays(1))
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();
```

Actually, avoid the awkward `ContinueWith` — use a plain `async` method instead. Replace the `SumAppliedTotalByChapterAsync` implementation above with:

```csharp
    public async Task<decimal> SumAppliedTotalByChapterAsync(int projectId, int chapterId) =>
        await _context.MaterialTickets
            .Where(t => t.ProjectId == projectId && t.ChapterId == chapterId && t.Status == MaterialTicketStatus.Applied)
            .SumAsync(t => (decimal?)t.Total) ?? 0m;
```

(`SumAsync` over an empty sequence of a nullable projection returns `null` instead of throwing — cast `t.Total` to `decimal?` for exactly that reason, then coalesce to `0m`.)

- [ ] **Step 5: Add the new method to `IPayrollRepository.cs`**

Add before the closing `}`:

```csharp
    Task<decimal> SumPaidTotalByChapterAsync(int projectId, int chapterId);
```

- [ ] **Step 6: Implement it in `PayrollRepository.cs`**

Add `using LvDomain.Enums;` to the `using` block, then add before the closing `}` of the class:

```csharp

    public async Task<decimal> SumPaidTotalByChapterAsync(int projectId, int chapterId) =>
        await _context.Payrolls
            .Where(p => p.ProjectId == projectId && p.ChapterId == chapterId && p.Status == PayrollStatus.Paid)
            .SumAsync(p => (decimal?)p.TotalPayroll) ?? 0m;
```

- [ ] **Step 7: Add the new method to `ISiteLogRepository.cs`**

Add before the closing `}`:

```csharp
    Task<List<SiteLog>> GetInRangeAsync(int projectId, DateTime from, DateTime to);
```

- [ ] **Step 8: Implement it in `SiteLogRepository.cs`**

Add before the closing `}` of the class:

```csharp

    public Task<List<SiteLog>> GetInRangeAsync(int projectId, DateTime from, DateTime to) =>
        _context.SiteLogs
            .Include(s => s.Workers)
            .Where(s => s.ProjectId == projectId && s.WeekStart >= from && s.WeekEnd <= to)
            .OrderBy(s => s.WeekStart)
            .ToListAsync();
```

- [ ] **Step 9: Add the new method to `IIncidentRepository.cs`**

Add before the closing `}`:

```csharp
    Task<(int Count, decimal TotalCost)> GetApprovedSummaryByChapterAsync(int projectId, int chapterId);
```

- [ ] **Step 10: Implement it in `IncidentRepository.cs`**

Add `using LvDomain.Enums;` to the `using` block, then add before the closing `}` of the class:

```csharp

    public async Task<(int Count, decimal TotalCost)> GetApprovedSummaryByChapterAsync(int projectId, int chapterId)
    {
        var matching = _context.Incidents
            .Where(i => i.ProjectId == projectId && i.ChapterId == chapterId && i.Status == IncidentStatus.Approved);

        var count = await matching.CountAsync();
        var totalCost = await matching.SumAsync(i => (decimal?)i.TotalCost) ?? 0m;

        return (count, totalCost);
    }
```

- [ ] **Step 11: Build to verify it compiles**

Run: `dotnet build LvTest/LvInfrastructure/LvInfrastructure.csproj`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 12: Run the full test suite to confirm no repository-level regressions**

Run: `dotnet test LvTest/LvTest/LvTest.csproj`
Expected: all 201 pre-existing tests still pass (these are pure additions — no existing method signature changed).

- [ ] **Step 13: Commit**

```bash
git add LvTest/LvApplication/Services/Projects/IProjectChapterRepository.cs LvTest/LvInfrastructure/Repositories/Projects/ProjectChapterRepository.cs LvTest/LvApplication/Services/Inventory/IMaterialTicketRepository.cs LvTest/LvInfrastructure/Repositories/Inventory/MaterialTicketRepository.cs LvTest/LvApplication/Services/Payroll/IPayrollRepository.cs LvTest/LvInfrastructure/Repositories/Payroll/PayrollRepository.cs LvTest/LvApplication/Services/SiteLogs/ISiteLogRepository.cs LvTest/LvInfrastructure/Repositories/SiteLogs/SiteLogRepository.cs LvTest/LvApplication/Services/Incidents/IIncidentRepository.cs LvTest/LvInfrastructure/Repositories/Incidents/IncidentRepository.cs
git commit -m "feat(chapters,finance): add repository methods for chapter cost aggregation and finance range queries"
```

---

### Task 4: DTOs (ChapterId retrofit + new ProjectChapter/Finance DTOs)

**Files:**
- Create: `LvApplication/DTOs/Projects/ProjectChapterDto.cs`, `UpdateAssignedSoldTotalDto.cs`
- Create: `LvApplication/DTOs/Finance/ProjectFinanceDto.cs`, `ProjectFinanceMaterialDto.cs`
- Modify: `LvApplication/DTOs/Inventory/CreateMaterialTicketDto.cs`, `UpdateMaterialTicketDto.cs`, `MaterialTicketDto.cs`
- Modify: `LvApplication/DTOs/Payroll/CreatePayrollDto.cs`, `UpdatePayrollDto.cs`, `PayrollDto.cs`
- Modify: `LvApplication/DTOs/SiteLogs/CreateSiteLogDto.cs`, `UpdateSiteLogDto.cs`, `SiteLogDto.cs`
- Modify: `LvApplication/DTOs/Incidents/CreateIncidentDto.cs`, `UpdateIncidentDto.cs`, `IncidentDto.cs`

**Interfaces:**
- Consumes: `FinancePeriod` (Task 1).
- Produces: `ProjectChapterDto { Id, ProjectId, ChapterId, AssignedSoldTotal, ActualCostTotal, ChapterProfit, IncidentCount, IncidentPercentage }`; `UpdateAssignedSoldTotalDto { AssignedSoldTotal }`; `ProjectFinanceDto { ProjectId, Period, PeriodStart, PeriodEnd, CurrentDirectExpenses, PendingExpenses, TotalHoursWorked, Materials }`; `ProjectFinanceMaterialDto { MaterialName, SupplierName, Quantity, Total, Date }`; `ChapterId` (nullable int) added to all 4 modules' Create/Update/response DTOs — Tasks 5, 6, 7, 8, 9 depend on these.

- [ ] **Step 1: Create the `ProjectChapter` DTOs**

`LvApplication/DTOs/Projects/ProjectChapterDto.cs`:
```csharp
namespace LvApplication.DTOs.Projects;

public class ProjectChapterDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int ChapterId { get; set; }
    public decimal AssignedSoldTotal { get; set; }
    public decimal ActualCostTotal { get; set; }
    public decimal ChapterProfit { get; set; }
    public int IncidentCount { get; set; }
    public decimal? IncidentPercentage { get; set; }
}
```

`LvApplication/DTOs/Projects/UpdateAssignedSoldTotalDto.cs`:
```csharp
namespace LvApplication.DTOs.Projects;

public class UpdateAssignedSoldTotalDto
{
    public decimal AssignedSoldTotal { get; set; }
}
```

- [ ] **Step 2: Create the Finance DTOs**

`LvApplication/DTOs/Finance/ProjectFinanceMaterialDto.cs`:
```csharp
namespace LvApplication.DTOs.Finance;

public class ProjectFinanceMaterialDto
{
    public string MaterialName { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Total { get; set; }
    public DateTime Date { get; set; }
}
```

`LvApplication/DTOs/Finance/ProjectFinanceDto.cs`:
```csharp
using LvDomain.Enums;

namespace LvApplication.DTOs.Finance;

public class ProjectFinanceDto
{
    public int ProjectId { get; set; }
    public FinancePeriod Period { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal CurrentDirectExpenses { get; set; }
    public decimal PendingExpenses { get; set; }
    public decimal TotalHoursWorked { get; set; }
    public List<ProjectFinanceMaterialDto> Materials { get; set; } = new();
}
```

- [ ] **Step 3: Add `ChapterId` to the MaterialTicket DTOs**

`CreateMaterialTicketDto.cs` and `UpdateMaterialTicketDto.cs` — add after `Discount`:
```csharp
    public int? ChapterId { get; set; }
```

`MaterialTicketDto.cs` — add after `Status`:
```csharp
    public int? ChapterId { get; set; }
```

- [ ] **Step 4: Add `ChapterId` to the Payroll DTOs**

`CreatePayrollDto.cs` and `UpdatePayrollDto.cs` — add after `SiteLogId`/at the top respectively:
```csharp
    public int? ChapterId { get; set; }
```
(`CreatePayrollDto` becomes `{ SiteLogId, ChapterId, Details }`; `UpdatePayrollDto` becomes `{ ChapterId, Details }`.)

`PayrollDto.cs` — add after `SiteLogId`:
```csharp
    public int? ChapterId { get; set; }
```

- [ ] **Step 5: Add `ChapterId` to the SiteLog DTOs**

`CreateSiteLogDto.cs` — add after `ProjectId`:
```csharp
    public int? ChapterId { get; set; }
```

`UpdateSiteLogDto.cs` — add at the top:
```csharp
    public int? ChapterId { get; set; }
```

`SiteLogDto.cs` — add after `ProjectId`:
```csharp
    public int? ChapterId { get; set; }
```

- [ ] **Step 6: Add `ChapterId` to the Incident DTOs**

`CreateIncidentDto.cs` — add after `ProjectId`:
```csharp
    public int? ChapterId { get; set; }
```

`UpdateIncidentDto.cs` — add at the top:
```csharp
    public int? ChapterId { get; set; }
```

`IncidentDto.cs` — add after `ProjectId`:
```csharp
    public int? ChapterId { get; set; }
```

- [ ] **Step 7: Build to verify it compiles**

Run: `dotnet build LvTest/LvApplication/LvApplication.csproj`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 8: Run the full test suite**

Run: `dotnet test LvTest/LvTest/LvTest.csproj`
Expected: all 201 tests still pass — every existing object initializer that builds these DTOs simply leaves the new `int?` property at its default `null`, which changes no observable behavior yet (nothing reads it until Task 7).

- [ ] **Step 9: Commit**

```bash
git add LvTest/LvApplication/DTOs/Projects/ProjectChapterDto.cs LvTest/LvApplication/DTOs/Projects/UpdateAssignedSoldTotalDto.cs LvTest/LvApplication/DTOs/Finance/ LvTest/LvApplication/DTOs/Inventory/CreateMaterialTicketDto.cs LvTest/LvApplication/DTOs/Inventory/UpdateMaterialTicketDto.cs LvTest/LvApplication/DTOs/Inventory/MaterialTicketDto.cs LvTest/LvApplication/DTOs/Payroll/CreatePayrollDto.cs LvTest/LvApplication/DTOs/Payroll/UpdatePayrollDto.cs LvTest/LvApplication/DTOs/Payroll/PayrollDto.cs LvTest/LvApplication/DTOs/SiteLogs/CreateSiteLogDto.cs LvTest/LvApplication/DTOs/SiteLogs/UpdateSiteLogDto.cs LvTest/LvApplication/DTOs/SiteLogs/SiteLogDto.cs LvTest/LvApplication/DTOs/Incidents/CreateIncidentDto.cs LvTest/LvApplication/DTOs/Incidents/UpdateIncidentDto.cs LvTest/LvApplication/DTOs/Incidents/IncidentDto.cs
git commit -m "feat(chapters,finance): add DTOs"
```

---

### Task 5: Validators (ChapterId structural rule + new UpdateAssignedSoldTotalDtoValidator)

**Files:**
- Create: `LvApplication/Validators/Projects/UpdateAssignedSoldTotalDtoValidator.cs`
- Modify: `LvApplication/Validators/Inventory/CreateMaterialTicketDtoValidator.cs`, `UpdateMaterialTicketDtoValidator.cs`
- Modify: `LvApplication/Validators/Payroll/CreatePayrollDtoValidator.cs`, `UpdatePayrollDtoValidator.cs`
- Modify: `LvApplication/Validators/SiteLogs/CreateSiteLogDtoValidator.cs`, `UpdateSiteLogDtoValidator.cs`
- Modify: `LvApplication/Validators/Incidents/CreateIncidentDtoValidator.cs`, `UpdateIncidentDtoValidator.cs`

**Interfaces:**
- Consumes: `UpdateAssignedSoldTotalDto` (Task 4).
- Produces: `IValidator<UpdateAssignedSoldTotalDto>`; structural `ChapterId > 0` rule (only enforced when provided) on the other 8 validators.

- [ ] **Step 1: Create `UpdateAssignedSoldTotalDtoValidator.cs`**

```csharp
using FluentValidation;
using LvApplication.DTOs.Projects;

namespace LvApplication.Validators.Projects;

public class UpdateAssignedSoldTotalDtoValidator : AbstractValidator<UpdateAssignedSoldTotalDto>
{
    public UpdateAssignedSoldTotalDtoValidator()
    {
        RuleFor(x => x.AssignedSoldTotal).GreaterThanOrEqualTo(0);
    }
}
```

- [ ] **Step 2: Add the `ChapterId` rule to the 8 existing validators**

In each of `CreateMaterialTicketDtoValidator.cs`, `UpdateMaterialTicketDtoValidator.cs`, `CreatePayrollDtoValidator.cs`, `UpdatePayrollDtoValidator.cs`, `CreateSiteLogDtoValidator.cs`, `UpdateSiteLogDtoValidator.cs`, `CreateIncidentDtoValidator.cs`, `UpdateIncidentDtoValidator.cs`, add this line inside the constructor (anywhere among the other `RuleFor` calls):

```csharp
        RuleFor(x => x.ChapterId).GreaterThan(0).When(x => x.ChapterId.HasValue);
```

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build LvTest/LvApplication/LvApplication.csproj`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Run the full test suite**

Run: `dotnet test LvTest/LvTest/LvTest.csproj`
Expected: all 201 tests still pass (the new rule is a no-op `.When()` guard for every DTO that leaves `ChapterId` unset).

- [ ] **Step 5: Commit**

```bash
git add LvTest/LvApplication/Validators/Projects/UpdateAssignedSoldTotalDtoValidator.cs LvTest/LvApplication/Validators/Inventory/CreateMaterialTicketDtoValidator.cs LvTest/LvApplication/Validators/Inventory/UpdateMaterialTicketDtoValidator.cs LvTest/LvApplication/Validators/Payroll/CreatePayrollDtoValidator.cs LvTest/LvApplication/Validators/Payroll/UpdatePayrollDtoValidator.cs LvTest/LvApplication/Validators/SiteLogs/CreateSiteLogDtoValidator.cs LvTest/LvApplication/Validators/SiteLogs/UpdateSiteLogDtoValidator.cs LvTest/LvApplication/Validators/Incidents/CreateIncidentDtoValidator.cs LvTest/LvApplication/Validators/Incidents/UpdateIncidentDtoValidator.cs
git commit -m "feat(chapters): add ChapterId validation rules"
```

---

### Task 6: `ProjectChapterService` + auto-creation in `ProjectService.CreateProjectAsync` + tests

**Files:**
- Create: `LvApplication/Services/Projects/IProjectChapterService.cs`, `ProjectChapterService.cs`
- Modify: `LvApplication/Services/Projects/ProjectService.cs`
- Modify: `LvTest/Common/ServiceFactory.cs`
- Modify: `LvTest/Services/Projects/ProjectServiceTests.cs`
- Test: `LvTest/Services/Projects/ProjectChapterServiceTests.cs`

**Interfaces:**
- Consumes: `IProjectChapterRepository`, `IMaterialTicketRepository.SumAppliedTotalByChapterAsync`, `IPayrollRepository.SumPaidTotalByChapterAsync`, `IIncidentRepository.GetApprovedSummaryByChapterAsync` (Tasks 3, 4).
- Produces: `IProjectChapterService { UpdateAssignedSoldTotalAsync(int,int,decimal) -> ProjectChapterDto, RecalculateActualCostAsync(int,int), GetByProjectAsync(int) -> List<ProjectChapterDto> }` — Task 7 (the 3 trigger points) and Task 9 (controller) depend on this.

- [ ] **Step 1: Create `IProjectChapterService.cs`**

```csharp
using LvApplication.DTOs.Projects;

namespace LvApplication.Services.Projects;

public interface IProjectChapterService
{
    Task<ProjectChapterDto> UpdateAssignedSoldTotalAsync(int projectId, int chapterId, decimal assignedSoldTotal);
    Task RecalculateActualCostAsync(int projectId, int chapterId);
    Task<List<ProjectChapterDto>> GetByProjectAsync(int projectId);
}
```

- [ ] **Step 2: Create `ProjectChapterService.cs`**

```csharp
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

    public ProjectChapterService(
        IProjectChapterRepository projectChapterRepository,
        IMaterialTicketRepository ticketRepository,
        IPayrollRepository payrollRepository,
        IIncidentRepository incidentRepository)
    {
        _projectChapterRepository = projectChapterRepository;
        _ticketRepository = ticketRepository;
        _payrollRepository = payrollRepository;
        _incidentRepository = incidentRepository;
    }

    public async Task<ProjectChapterDto> UpdateAssignedSoldTotalAsync(int projectId, int chapterId, decimal assignedSoldTotal)
    {
        var projectChapter = await _projectChapterRepository.GetByProjectAndChapterAsync(projectId, chapterId)
            ?? throw new NotFoundException($"No hay un ProjectChapter para el proyecto {projectId} y el capítulo {chapterId}.");

        projectChapter.AssignedSoldTotal = assignedSoldTotal;
        projectChapter.ChapterProfit = assignedSoldTotal - projectChapter.ActualCostTotal;
        projectChapter.UpdatedAt = DateTime.UtcNow;

        await _projectChapterRepository.UpdateAsync(projectChapter);

        return MapToDto(projectChapter);
    }

    public async Task RecalculateActualCostAsync(int projectId, int chapterId)
    {
        var projectChapter = await _projectChapterRepository.GetByProjectAndChapterAsync(projectId, chapterId)
            ?? throw new NotFoundException($"No hay un ProjectChapter para el proyecto {projectId} y el capítulo {chapterId}.");

        var ticketsTotal = await _ticketRepository.SumAppliedTotalByChapterAsync(projectId, chapterId);
        var payrollTotal = await _payrollRepository.SumPaidTotalByChapterAsync(projectId, chapterId);
        var (incidentCount, incidentsTotal) = await _incidentRepository.GetApprovedSummaryByChapterAsync(projectId, chapterId);

        // NO se incluye SiteLog.TotalMaterials aquí: ese costo ya queda contabilizado a través
        // de MaterialTicket.ApplyAsync — sumarlo de nuevo sería doble conteo, mismo criterio ya
        // usado para Project.CurrentDirectExpenses en fases anteriores.
        projectChapter.ActualCostTotal = ticketsTotal + payrollTotal + incidentsTotal;
        projectChapter.ChapterProfit = projectChapter.AssignedSoldTotal - projectChapter.ActualCostTotal;
        projectChapter.IncidentCount = incidentCount;
        projectChapter.IncidentPercentage = projectChapter.ActualCostTotal == 0
            ? null
            : incidentsTotal / projectChapter.ActualCostTotal * 100m;
        projectChapter.UpdatedAt = DateTime.UtcNow;

        await _projectChapterRepository.UpdateAsync(projectChapter);
    }

    public async Task<List<ProjectChapterDto>> GetByProjectAsync(int projectId)
    {
        var chapters = await _projectChapterRepository.GetByProjectAsync(projectId);
        return chapters.Select(MapToDto).ToList();
    }

    private static ProjectChapterDto MapToDto(ProjectChapter chapter) => new()
    {
        Id = chapter.Id,
        ProjectId = chapter.ProjectId,
        ChapterId = chapter.ChapterId,
        AssignedSoldTotal = chapter.AssignedSoldTotal,
        ActualCostTotal = chapter.ActualCostTotal,
        ChapterProfit = chapter.ChapterProfit,
        IncidentCount = chapter.IncidentCount,
        IncidentPercentage = chapter.IncidentPercentage
    };
}
```

- [ ] **Step 3: Wire auto-creation into `ProjectService.CreateProjectAsync`**

Add `IProjectChapterRepository projectChapterRepository` as a new constructor parameter (insert after `workerRepository`):

```csharp
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
```

In `CreateProjectAsync`, replace:
```csharp
        await _projectRepository.AddAsync(project);

        return MapToDto(project);
    }
```

with:
```csharp
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
```

Note: `LvDomain.Entities.Projects.ProjectChapter` is fully qualified here because this file's own namespace (`LvApplication.Services.Projects`) does not collide with it (different identifier, "Project" vs "ProjectChapter" vs "Projects" — no ambiguity risk here unlike the `Payroll`/`Incident` situation from earlier phases), but being explicit removes any doubt since `Project` (the class) is already the bare name in heavy use in this exact file.

- [ ] **Step 4: Update `LvTest/Common/ServiceFactory.cs`**

Add to the `using` block (if not already present):
```csharp
using LvInfrastructure.Repositories.Projects;
```

Add this method (anywhere before `CreateProjectService`, since it has no other dependencies):
```csharp

    public static ProjectChapterService CreateProjectChapterService(AppDbContext context) =>
        new(
            new ProjectChapterRepository(context),
            new MaterialTicketRepository(context),
            new PayrollRepository(context),
            new IncidentRepository(context));
```

Update `CreateProjectService` to pass the new dependency:
```csharp
    public static ProjectService CreateProjectService(AppDbContext context) =>
        new(
            new ProjectRepository(context),
            new OfferRepository(context),
            new BudgetRepository(context),
            new BranchRepository(context),
            new WorkerRepository(context),
            new ProjectChapterRepository(context),
            new CreateProjectDtoValidator(),
            new UpdateEndDateDtoValidator(),
            new AssignWorkerDtoValidator());
```

- [ ] **Step 5: Write the failing tests in `LvTest/Services/Projects/ProjectServiceTests.cs`**

Add `using LvDomain.Entities.Budgets;` to the `using` block if not already present, then append these two methods inside the class (before the final closing `}`):

```csharp

    [Fact]
    public async Task CreateProjectAsync_TurnKey_AutoCreatesProjectChaptersWithProportionalAssignedSoldTotal()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, $"director-{Guid.NewGuid():N}@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, $"gm-{Guid.NewGuid():N}@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateBudgetAsync(context, customer.Id, branch.Id, creator.Id, BudgetStatus.ClientApproved);

        var chapter1 = new BudgetChapter { BudgetId = budget.Id, Name = "Cimentación", Order = 1, TotalChapter = 400m, EstimatedWeeks = 4, CreatedAt = DateTime.UtcNow };
        var chapter2 = new BudgetChapter { BudgetId = budget.Id, Name = "Estructura", Order = 2, TotalChapter = 600m, EstimatedWeeks = 6, CreatedAt = DateTime.UtcNow };
        context.BudgetChapters.AddRange(chapter1, chapter2);
        await context.SaveChangesAsync();

        var offer = await CreateOfferAsync(context, budget.Id, customer.Id, creator.Id, OfferStatus.ClientAccepted); // OfferType.Turnkey, TotalProjectPrice = 100000m
        var service = ServiceFactory.CreateProjectService(context);

        var project = await service.CreateProjectAsync(BuildCreateDto(offer.Id, branch.Id), creator.Id);

        var projectChapters = await context.ProjectChapters
            .Where(pc => pc.ProjectId == project.Id)
            .OrderBy(pc => pc.ChapterId)
            .ToListAsync();

        projectChapters.Should().HaveCount(2);
        // budget.TotalBudget = 1000 (CreateBudgetAsync); weights 400/1000 and 600/1000 of TotalProjectPrice = 100000.
        projectChapters.Should().ContainSingle(pc => pc.ChapterId == chapter1.Id && pc.AssignedSoldTotal == 40000m);
        projectChapters.Should().ContainSingle(pc => pc.ChapterId == chapter2.Id && pc.AssignedSoldTotal == 60000m);
    }

    [Fact]
    public async Task CreateProjectAsync_Percentage_AssignedSoldTotalStartsAtZero()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, $"director-{Guid.NewGuid():N}@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, $"gm-{Guid.NewGuid():N}@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateBudgetAsync(context, customer.Id, branch.Id, creator.Id, BudgetStatus.ClientApproved);

        var chapter = new BudgetChapter { BudgetId = budget.Id, Name = "Cimentación", Order = 1, TotalChapter = 400m, EstimatedWeeks = 4, CreatedAt = DateTime.UtcNow };
        context.BudgetChapters.Add(chapter);
        await context.SaveChangesAsync();

        var offer = new Offer
        {
            BudgetId = budget.Id,
            CustomerId = customer.Id,
            OfferNumber = $"OF-TEST-{Guid.NewGuid():N}",
            OfferType = OfferType.Percentage,
            IssueDate = new DateTime(2026, 1, 10),
            ValidityDays = 30,
            WorkLocation = "San Jose Centro",
            WorkScope = "Construccion de edificio de 3 niveles",
            EstimatedStartDate = new DateTime(2026, 2, 1),
            EstimatedDurationWeeks = 10,
            EstimatedDeliveryDate = new DateTime(2026, 2, 1).AddDays(10 * 7),
            PaymentTerms = "50% inicio, 50% entrega",
            Warranties = "1 año estructural",
            Exclusions = "No incluye mobiliario",
            AgreedPercentage = 10m,
            Status = OfferStatus.ClientAccepted,
            CreatedByUserId = creator.Id,
            CreatedAt = DateTime.UtcNow
        };
        context.Offers.Add(offer);
        await context.SaveChangesAsync();

        var service = ServiceFactory.CreateProjectService(context);
        var project = await service.CreateProjectAsync(BuildCreateDto(offer.Id, branch.Id), creator.Id);

        var projectChapters = await context.ProjectChapters.Where(pc => pc.ProjectId == project.Id).ToListAsync();
        projectChapters.Should().ContainSingle(pc => pc.ChapterId == chapter.Id && pc.AssignedSoldTotal == 0m);
    }
```

- [ ] **Step 6: Write `ProjectChapterServiceTests.cs`**

This test file needs its own project/budget/chapter/offer scaffolding (same pattern as every other test file in this codebase). Create `LvTest/Services/Projects/ProjectChapterServiceTests.cs`:

```csharp
using FluentAssertions;
using LvApplication.DTOs.Incidents;
using LvApplication.DTOs.Payroll;
using LvApplication.DTOs.Projects;
using LvApplication.DTOs.SiteLogs;
using LvDomain.Entities.Branches;
using LvDomain.Entities.Budgets;
using LvDomain.Entities.Customers;
using LvDomain.Entities.Inventory;
using LvDomain.Entities.Materials;
using LvDomain.Entities.Offers;
using LvDomain.Entities.Suppliers;
using LvDomain.Entities.Workers;
using LvDomain.Enums;
using LvInfrastructure.Persistence;
using LvTest.Common;
using Microsoft.EntityFrameworkCore;

namespace LvTest.Services.Projects;

public class ProjectChapterServiceTests
{
    private static async Task<Customer> CreateProjectCustomerAsync(AppDbContext context)
    {
        var customer = new Customer { Name = "Project Customer", CustomerType = CustomerType.Project, Status = ActiveStatus.Active, CreatedAt = DateTime.UtcNow };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer;
    }

    private static async Task<Branch> CreateBranchAsync(AppDbContext context, int operationsDirectorId)
    {
        var branch = new Branch { Name = "Test Branch", City = "San Jose", Province = "San Jose", Status = BranchStatus.Active, BranchType = BranchType.Office, OperationsDirectorId = operationsDirectorId, CreatedAt = DateTime.UtcNow };
        context.Branches.Add(branch);
        await context.SaveChangesAsync();
        return branch;
    }

    private static async Task<Supplier> CreateSupplierAsync(AppDbContext context)
    {
        var supplier = new Supplier { Name = "Proveedor Test", Status = ActiveStatus.Active, CreatedAt = DateTime.UtcNow };
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();
        return supplier;
    }

    private static async Task<MaterialCatalog> CreateMaterialAsync(AppDbContext context)
    {
        var material = new MaterialCatalog { Name = "Cemento", CreatedAt = DateTime.UtcNow };
        context.MaterialCatalogs.Add(material);
        await context.SaveChangesAsync();
        return material;
    }

    private static async Task<(ProjectDto Project, BudgetChapter Chapter, int ManagerId, int ProjectAdminId)> CreateProjectWithChapterAsync(AppDbContext context)
    {
        var director = await TestUserFactory.CreateAsync(context, $"director-{Guid.NewGuid():N}@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var manager = await TestUserFactory.CreateAsync(context, $"gm-{Guid.NewGuid():N}@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var projectAdmin = await TestUserFactory.CreateAsync(context, $"pa-{Guid.NewGuid():N}@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);

        var budget = new Budget
        {
            CustomerId = customer.Id,
            BranchId = branch.Id,
            Name = "Edificio Test",
            Status = BudgetStatus.ClientApproved,
            UtilityPercentage = 10,
            IndirectCostsTotal = 50,
            TotalBudget = 1000,
            CreatedByUserId = manager.Id,
            CreatedAt = DateTime.UtcNow
        };
        context.Budgets.Add(budget);
        await context.SaveChangesAsync();

        var chapter = new BudgetChapter { BudgetId = budget.Id, Name = "Cimentación", Order = 1, TotalChapter = 1000m, EstimatedWeeks = 10, CreatedAt = DateTime.UtcNow };
        context.BudgetChapters.Add(chapter);
        await context.SaveChangesAsync();

        var offer = new Offer
        {
            BudgetId = budget.Id,
            CustomerId = customer.Id,
            OfferNumber = $"OF-TEST-{Guid.NewGuid():N}",
            OfferType = OfferType.Turnkey,
            IssueDate = new DateTime(2026, 1, 10),
            ValidityDays = 30,
            WorkLocation = "San Jose Centro",
            WorkScope = "Construccion de edificio de 3 niveles",
            EstimatedStartDate = new DateTime(2026, 2, 1),
            EstimatedDurationWeeks = 10,
            EstimatedDeliveryDate = new DateTime(2026, 2, 1).AddDays(10 * 7),
            PaymentTerms = "50% inicio, 50% entrega",
            Warranties = "1 año estructural",
            Exclusions = "No incluye mobiliario",
            TotalProjectPrice = 100000m,
            Status = OfferStatus.ClientAccepted,
            CreatedByUserId = manager.Id,
            CreatedAt = DateTime.UtcNow
        };
        context.Offers.Add(offer);
        await context.SaveChangesAsync();

        var projectService = ServiceFactory.CreateProjectService(context);
        var project = await projectService.CreateProjectAsync(new CreateProjectDto
        {
            OfferId = offer.Id,
            BranchId = branch.Id,
            StartDate = new DateTime(2026, 3, 1)
        }, manager.Id);

        return (project, chapter, manager.Id, projectAdmin.Id);
    }

    [Fact]
    public async Task RecalculateActualCostAsync_SumsAllThreeSourcesFilteredByChapter_ExcludingSiteLog()
    {
        using var context = TestDbContextFactory.Create();
        var (project, chapter, managerId, projectAdminId) = await CreateProjectWithChapterAsync(context);
        var supplier = await CreateSupplierAsync(context);
        var material = await CreateMaterialAsync(context);

        var ticketService = ServiceFactory.CreateMaterialTicketService(context);
        var ticket = await ticketService.CreateAsync(project.Id, new LvApplication.DTOs.Inventory.CreateMaterialTicketDto
        {
            SupplierId = supplier.Id,
            MaterialId = material.Id,
            Quantity = 10,
            UnitPrice = 5,
            ChapterId = chapter.Id
        }, projectAdminId); // Total = 50
        await ticketService.ApplyAsync(ticket.Id);

        var siteLogService = ServiceFactory.CreateSiteLogService(context);
        var siteLog = await siteLogService.CreateAsync(new CreateSiteLogDto
        {
            ProjectId = project.Id,
            WeekStart = new DateTime(2026, 3, 2),
            WeekEnd = new DateTime(2026, 3, 8),
            TaskDescription = "Semana de trabajo",
            ChapterId = chapter.Id
        }, projectAdminId);
        await siteLogService.SubmitToReviewAsync(siteLog.Id);
        await siteLogService.ApproveAsync(siteLog.Id, managerId); // TotalMaterials should NOT be counted

        var payrollService = ServiceFactory.CreatePayrollService(context);
        var payroll = await payrollService.CreateAsync(new CreatePayrollDto
        {
            SiteLogId = siteLog.Id,
            ChapterId = chapter.Id,
            Details = new()
        }, projectAdminId); // TotalPayroll = 0, but still exercises the chapter-filtered sum path
        await payrollService.MarkAsPaidAsync(payroll.Id);

        var incidentService = ServiceFactory.CreateIncidentService(context);
        var incident = await incidentService.CreateAsync(new CreateIncidentDto
        {
            ProjectId = project.Id,
            Date = new DateTime(2026, 3, 5),
            Description = "Imprevisto de prueba",
            ChapterId = chapter.Id,
            Materials = new(),
            Workers = new()
        }, projectAdminId); // TotalCost = 0
        await incidentService.ApproveAsync(incident.Id, managerId);

        var projectChapter = await context.ProjectChapters.FirstAsync(pc => pc.ProjectId == project.Id && pc.ChapterId == chapter.Id);

        projectChapter.ActualCostTotal.Should().Be(50m); // only the ticket contributes a non-zero amount
        projectChapter.ChapterProfit.Should().Be(projectChapter.AssignedSoldTotal - 50m);
        projectChapter.IncidentCount.Should().Be(1);
        projectChapter.IncidentPercentage.Should().Be(0m); // 0 / 50 * 100
    }

    [Fact]
    public async Task UpdateAssignedSoldTotalAsync_UpdatesValueAndRecalculatesChapterProfit()
    {
        using var context = TestDbContextFactory.Create();
        var (project, chapter, _, _) = await CreateProjectWithChapterAsync(context);
        var service = ServiceFactory.CreateProjectChapterService(context);

        var updated = await service.UpdateAssignedSoldTotalAsync(project.Id, chapter.Id, 12345m);

        updated.AssignedSoldTotal.Should().Be(12345m);
        updated.ChapterProfit.Should().Be(12345m); // ActualCostTotal is still 0 at this point
    }
}
```

- [ ] **Step 7: Run the tests**

Run: `dotnet test LvTest/LvTest/LvTest.csproj --filter "FullyQualifiedName~ProjectServiceTests|FullyQualifiedName~ProjectChapterServiceTests"`
Expected: all pass, including the 4 new tests. (This step runs before Task 7's `ChapterId` wiring is added to the 4 services — if run strictly in isolation before Task 7, `CreateMaterialTicketDto`/`CreateSiteLogDto`/`CreatePayrollDto`/`CreateIncidentDto`'s `ChapterId` will compile [Task 4] but the services won't validate or persist it correctly [`ticket.ChapterId` will save fine via EF regardless, since it's a plain entity property set — but the `RecalculateActualCostAsync` triggers from Task 7 won't fire yet]. Practically: run Task 6 and Task 7 back-to-back before trusting this test, since `RecalculateActualCostAsync_...` depends on `ApplyAsync`/`MarkAsPaidAsync`/`ApproveAsync` calling it — see Task 7 Step 6.)

- [ ] **Step 8: Commit**

```bash
git add LvTest/LvApplication/Services/Projects/IProjectChapterService.cs LvTest/LvApplication/Services/Projects/ProjectChapterService.cs LvTest/LvApplication/Services/Projects/ProjectService.cs LvTest/LvTest/Common/ServiceFactory.cs LvTest/LvTest/Services/Projects/ProjectServiceTests.cs LvTest/LvTest/Services/Projects/ProjectChapterServiceTests.cs
git commit -m "feat(chapters): add ProjectChapterService and auto-creation on Project creation"
```

---

### Task 7: Chapter validation + recalculation triggers in the 4 retrofitted services

**Files:**
- Modify: `LvApplication/Services/Inventory/MaterialTicketService.cs`
- Modify: `LvApplication/Services/Payroll/PayrollService.cs`
- Modify: `LvApplication/Services/SiteLogs/SiteLogService.cs`
- Modify: `LvApplication/Services/Incidents/IncidentService.cs`
- Modify: `LvTest/Common/ServiceFactory.cs`

**Interfaces:**
- Consumes: `IBudgetRepository` (pre-existing), `IProjectChapterService.RecalculateActualCostAsync` (Task 6).
- Produces: chapter-membership validation + auto-recalculation wired into `MaterialTicketService.{CreateAsync,UpdateAsync,ApplyAsync}`, `PayrollService.{CreateAsync,UpdateAsync,MarkAsPaidAsync}`, `SiteLogService.{CreateAsync,UpdateAsync}` (validation only, no trigger), `IncidentService.{CreateAsync,UpdateAsync,ApproveAsync}` — Task 6's `ProjectChapterServiceTests` and this task's own tests depend on this.

- [ ] **Step 1: Retrofit `MaterialTicketService.cs`**

Add to the `using` block:
```csharp
using LvApplication.Services.Budgets;
using LvApplication.Services.Projects;
```

Add two new fields/constructor params (insert `budgetRepository` after `projectRepository`, `projectChapterService` after `materialCatalogRepository`):
```csharp
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
        IValidator<UpdateMaterialTicketDto> updateValidator)
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
```

In `CreateAsync`, right after the `project` fetch, add:
```csharp
        await ValidateChapterAsync(project.BudgetId, request.ChapterId);
```

And add `ChapterId = request.ChapterId,` to the `MaterialTicket` object initializer (anywhere among the other properties).

In `UpdateAsync`, the method currently fetches `project` only near the end (for the `PendingExpenses` adjustment). Move that fetch earlier — replace:
```csharp
        var supplier = await _supplierRepository.GetByIdAsync(request.SupplierId)
            ?? throw new NotFoundException($"Supplier {request.SupplierId} not found.");

        var material = await _materialCatalogRepository.GetByIdAsync(request.MaterialId)
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
        ticket.UpdatedAt = DateTime.UtcNow;

        await _ticketRepository.UpdateAsync(ticket);

        var project = await _projectRepository.GetByIdAsync(ticket.ProjectId)
            ?? throw new NotFoundException($"Project {ticket.ProjectId} not found.");

        project.PendingExpenses += ticket.Total - previousTotal;
        project.UpdatedAt = DateTime.UtcNow;
        await _projectRepository.UpdateAsync(project);

        return MapToDto(ticket);
    }
```

with:
```csharp
        var project = await _projectRepository.GetByIdAsync(ticket.ProjectId)
            ?? throw new NotFoundException($"Project {ticket.ProjectId} not found.");

        await ValidateChapterAsync(project.BudgetId, request.ChapterId);

        var supplier = await _supplierRepository.GetByIdAsync(request.SupplierId)
            ?? throw new NotFoundException($"Supplier {request.SupplierId} not found.");

        var material = await _materialCatalogRepository.GetByIdAsync(request.MaterialId)
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
```

In `ApplyAsync`, right before `return MapToDto(ticket);`, add:
```csharp

        if (ticket.ChapterId.HasValue)
        {
            await _projectChapterService.RecalculateActualCostAsync(ticket.ProjectId, ticket.ChapterId.Value);
        }
```

Add `ChapterId = ticket.ChapterId,` to `MapToDto`'s object initializer.

Add this private helper at the end of the class (before the closing `}`):
```csharp

    private async Task ValidateChapterAsync(int budgetId, int? chapterId)
    {
        if (!chapterId.HasValue)
        {
            return;
        }

        var budget = await _budgetRepository.GetByIdAsync(budgetId)
            ?? throw new NotFoundException($"Budget {budgetId} not found.");

        if (!budget.Chapters.Any(c => c.Id == chapterId.Value))
        {
            throw new ValidationAppException($"El capítulo {chapterId} no pertenece al presupuesto de este proyecto.");
        }
    }
```

- [ ] **Step 2: Retrofit `PayrollService.cs`**

Add to the `using` block:
```csharp
using LvApplication.Services.Budgets;
```

Add two new fields/constructor params:
```csharp
    private readonly IPayrollRepository _payrollRepository;
    private readonly ISiteLogService _siteLogService;
    private readonly IProjectRepository _projectRepository;
    private readonly IBudgetRepository _budgetRepository;
    private readonly IProjectService _projectService;
    private readonly IProjectChapterService _projectChapterService;
    private readonly IValidator<CreatePayrollDto> _createValidator;
    private readonly IValidator<UpdatePayrollDto> _updateValidator;

    public PayrollService(
        IPayrollRepository payrollRepository,
        ISiteLogService siteLogService,
        IProjectRepository projectRepository,
        IBudgetRepository budgetRepository,
        IProjectService projectService,
        IProjectChapterService projectChapterService,
        IValidator<CreatePayrollDto> createValidator,
        IValidator<UpdatePayrollDto> updateValidator)
    {
        _payrollRepository = payrollRepository;
        _siteLogService = siteLogService;
        _projectRepository = projectRepository;
        _budgetRepository = budgetRepository;
        _projectService = projectService;
        _projectChapterService = projectChapterService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }
```

(`IProjectChapterService` lives in `LvApplication.Services.Projects`, already imported via the existing `using LvApplication.Services.Projects;` line.)

In `CreateAsync`, right after the duplicate-`SiteLogId` check and before constructing the `Payroll` object, add:
```csharp
        if (request.ChapterId.HasValue)
        {
            var project = await _projectRepository.GetByIdAsync(siteLog.ProjectId)
                ?? throw new NotFoundException($"Project {siteLog.ProjectId} not found.");
            await ValidateChapterAsync(project.BudgetId, request.ChapterId);
        }
```

Add `ChapterId = request.ChapterId,` to the `Payroll` object initializer.

In `UpdateAsync`, right after the `Status != Pending` check, add:
```csharp
        if (request.ChapterId.HasValue)
        {
            var project = await _projectRepository.GetByIdAsync(payroll.ProjectId)
                ?? throw new NotFoundException($"Project {payroll.ProjectId} not found.");
            await ValidateChapterAsync(project.BudgetId, request.ChapterId);
        }

        payroll.ChapterId = request.ChapterId;
```

(placed right before the existing `payroll.UpdatedAt = DateTime.UtcNow;` line).

In `MarkAsPaidAsync`, right before `return MapToDto(payroll);`, add:
```csharp

        if (payroll.ChapterId.HasValue)
        {
            await _projectChapterService.RecalculateActualCostAsync(payroll.ProjectId, payroll.ChapterId.Value);
        }
```

Add `ChapterId = payroll.ChapterId,` to `MapToDto`'s object initializer.

Add this private helper at the end of the class (before the closing `}`):
```csharp

    private async Task ValidateChapterAsync(int budgetId, int? chapterId)
    {
        if (!chapterId.HasValue)
        {
            return;
        }

        var budget = await _budgetRepository.GetByIdAsync(budgetId)
            ?? throw new NotFoundException($"Budget {budgetId} not found.");

        if (!budget.Chapters.Any(c => c.Id == chapterId.Value))
        {
            throw new ValidationAppException($"El capítulo {chapterId} no pertenece al presupuesto de este proyecto.");
        }
    }
```

- [ ] **Step 3: Retrofit `SiteLogService.cs`**

Add to the `using` block:
```csharp
using LvApplication.Services.Budgets;
```

Add one new field/constructor param (`IBudgetRepository budgetRepository`, inserted after `inventoryRepository`):
```csharp
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
        IValidator<UpdateSiteLogDto> updateValidator)
    {
        _siteLogRepository = siteLogRepository;
        _projectRepository = projectRepository;
        _inventoryRepository = inventoryRepository;
        _budgetRepository = budgetRepository;
        _projectProgressService = projectProgressService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }
```

In `CreateAsync`, right after the duplicate-week check and before constructing the `SiteLog` object, add:
```csharp
        await ValidateChapterAsync(project.BudgetId, request.ChapterId);
```

Add `ChapterId = request.ChapterId,` to the `SiteLog` object initializer.

In `UpdateAsync`, right after `EnsureEditable(siteLog);`, add:
```csharp

        if (request.ChapterId.HasValue)
        {
            var project = await _projectRepository.GetByIdAsync(siteLog.ProjectId)
                ?? throw new NotFoundException($"Project {siteLog.ProjectId} not found.");
            await ValidateChapterAsync(project.BudgetId, request.ChapterId);
        }

        siteLog.ChapterId = request.ChapterId;
```

(this replaces the blank line before `siteLog.TaskDescription = request.TaskDescription;` — keep that line and everything after it unchanged).

Add `ChapterId = siteLog.ChapterId,` to `MapToDto`'s object initializer.

Add this private helper at the end of the class (before the closing `}`):
```csharp

    private async Task ValidateChapterAsync(int budgetId, int? chapterId)
    {
        if (!chapterId.HasValue)
        {
            return;
        }

        var budget = await _budgetRepository.GetByIdAsync(budgetId)
            ?? throw new NotFoundException($"Budget {budgetId} not found.");

        if (!budget.Chapters.Any(c => c.Id == chapterId.Value))
        {
            throw new ValidationAppException($"El capítulo {chapterId} no pertenece al presupuesto de este proyecto.");
        }
    }
```

- [ ] **Step 4: Retrofit `IncidentService.cs`**

Add to the `using` block:
```csharp
using LvApplication.Services.Budgets;
using LvApplication.Services.Projects;
```

Add two new fields/constructor params:
```csharp
    private readonly IIncidentRepository _incidentRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IBudgetRepository _budgetRepository;
    private readonly IProjectInventoryItemRepository _inventoryRepository;
    private readonly IWorkerRepository _workerRepository;
    private readonly IProjectChapterService _projectChapterService;
    private readonly IValidator<CreateIncidentDto> _createValidator;
    private readonly IValidator<UpdateIncidentDto> _updateValidator;

    public IncidentService(
        IIncidentRepository incidentRepository,
        IProjectRepository projectRepository,
        IBudgetRepository budgetRepository,
        IProjectInventoryItemRepository inventoryRepository,
        IWorkerRepository workerRepository,
        IProjectChapterService projectChapterService,
        IValidator<CreateIncidentDto> createValidator,
        IValidator<UpdateIncidentDto> updateValidator)
    {
        _incidentRepository = incidentRepository;
        _projectRepository = projectRepository;
        _budgetRepository = budgetRepository;
        _inventoryRepository = inventoryRepository;
        _workerRepository = workerRepository;
        _projectChapterService = projectChapterService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }
```

In `CreateAsync`, right after the `project` fetch, add:
```csharp
        await ValidateChapterAsync(project.BudgetId, request.ChapterId);
```

Add `ChapterId = request.ChapterId,` to the `Incident` object initializer.

In `UpdateAsync`, right after the `Status != Draft` check, add:
```csharp

        if (request.ChapterId.HasValue)
        {
            var project = await _projectRepository.GetByIdAsync(incident.ProjectId)
                ?? throw new NotFoundException($"Project {incident.ProjectId} not found.");
            await ValidateChapterAsync(project.BudgetId, request.ChapterId);
        }

        incident.ChapterId = request.ChapterId;
```

(placed right before the existing `var previousTotalCost = incident.TotalCost;` line).

In `ApproveAsync`, right before `return MapToDto(incident);`, add:
```csharp

        if (incident.ChapterId.HasValue)
        {
            await _projectChapterService.RecalculateActualCostAsync(incident.ProjectId, incident.ChapterId.Value);
        }
```

Add `ChapterId = incident.ChapterId,` to `MapToDto`'s object initializer.

Add this private helper at the end of the class (before the closing `}`):
```csharp

    private async Task ValidateChapterAsync(int budgetId, int? chapterId)
    {
        if (!chapterId.HasValue)
        {
            return;
        }

        var budget = await _budgetRepository.GetByIdAsync(budgetId)
            ?? throw new NotFoundException($"Budget {budgetId} not found.");

        if (!budget.Chapters.Any(c => c.Id == chapterId.Value))
        {
            throw new ValidationAppException($"El capítulo {chapterId} no pertenece al presupuesto de este proyecto.");
        }
    }
```

- [ ] **Step 5: Update `LvTest/Common/ServiceFactory.cs`** to pass the new dependencies

Update the four factory methods:
```csharp
    public static MaterialTicketService CreateMaterialTicketService(AppDbContext context) =>
        new(
            new MaterialTicketRepository(context),
            new ProjectInventoryItemRepository(context),
            new ProjectRepository(context),
            new BudgetRepository(context),
            new SupplierRepository(context),
            new MaterialCatalogRepository(context),
            CreateProjectChapterService(context),
            new CreateMaterialTicketDtoValidator(),
            new UpdateMaterialTicketDtoValidator());

    public static SiteLogService CreateSiteLogService(AppDbContext context) =>
        new(
            new SiteLogRepository(context),
            new ProjectRepository(context),
            new ProjectInventoryItemRepository(context),
            new BudgetRepository(context),
            CreateProjectProgressService(context),
            new CreateSiteLogDtoValidator(),
            new UpdateSiteLogDtoValidator());

    public static PayrollService CreatePayrollService(AppDbContext context) =>
        new(
            new PayrollRepository(context),
            CreateSiteLogService(context),
            new ProjectRepository(context),
            new BudgetRepository(context),
            CreateProjectService(context),
            CreateProjectChapterService(context),
            new CreatePayrollDtoValidator(),
            new UpdatePayrollDtoValidator());

    public static IncidentService CreateIncidentService(AppDbContext context) =>
        new(
            new IncidentRepository(context),
            new ProjectRepository(context),
            new BudgetRepository(context),
            new ProjectInventoryItemRepository(context),
            new WorkerRepository(context),
            CreateProjectChapterService(context),
            new CreateIncidentDtoValidator(),
            new UpdateIncidentDtoValidator());
```

- [ ] **Step 6: Run the tests**

Run: `dotnet test LvTest/LvTest/LvTest.csproj`
Expected: **all 201 pre-existing tests + the 4 new `ProjectServiceTests`/`ProjectChapterServiceTests` tests from Task 6 all pass now** (Task 6's `RecalculateActualCostAsync_...` test needed this task's triggers to actually fire — this is the step where that test goes green).

- [ ] **Step 7: Commit**

```bash
git add LvTest/LvApplication/Services/Inventory/MaterialTicketService.cs LvTest/LvApplication/Services/Payroll/PayrollService.cs LvTest/LvApplication/Services/SiteLogs/SiteLogService.cs LvTest/LvApplication/Services/Incidents/IncidentService.cs LvTest/LvTest/Common/ServiceFactory.cs
git commit -m "feat(chapters): validate ChapterId against project budget and trigger recalculation on Apply/MarkAsPaid/Approve"
```

---

### Task 8: `ProjectFinanceService` + tests

**Files:**
- Create: `LvApplication/Services/Finance/IProjectFinanceService.cs`, `ProjectFinanceService.cs`
- Modify: `LvTest/Common/ServiceFactory.cs`
- Test: `LvTest/Services/Finance/ProjectFinanceServiceTests.cs`

**Interfaces:**
- Consumes: `IProjectRepository.GetByIdAsync` (pre-existing), `IMaterialTicketRepository.GetAppliedInRangeAsync`, `ISiteLogRepository.GetInRangeAsync` (Task 3).
- Produces: `IProjectFinanceService { GetFinanceAsync(int,FinancePeriod,DateTime) -> ProjectFinanceDto }` — Task 9 (controller) depends on this.

- [ ] **Step 1: Create `IProjectFinanceService.cs`**

```csharp
using LvApplication.DTOs.Finance;
using LvDomain.Enums;

namespace LvApplication.Services.Finance;

public interface IProjectFinanceService
{
    Task<ProjectFinanceDto> GetFinanceAsync(int projectId, FinancePeriod period, DateTime date);
}
```

- [ ] **Step 2: Create `ProjectFinanceService.cs`**

```csharp
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Finance;
using LvApplication.Services.Inventory;
using LvApplication.Services.Projects;
using LvApplication.Services.SiteLogs;
using LvDomain.Enums;

namespace LvApplication.Services.Finance;

public class ProjectFinanceService : IProjectFinanceService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IMaterialTicketRepository _ticketRepository;
    private readonly ISiteLogRepository _siteLogRepository;

    public ProjectFinanceService(
        IProjectRepository projectRepository,
        IMaterialTicketRepository ticketRepository,
        ISiteLogRepository siteLogRepository)
    {
        _projectRepository = projectRepository;
        _ticketRepository = ticketRepository;
        _siteLogRepository = siteLogRepository;
    }

    public async Task<ProjectFinanceDto> GetFinanceAsync(int projectId, FinancePeriod period, DateTime date)
    {
        var project = await _projectRepository.GetByIdAsync(projectId)
            ?? throw new NotFoundException($"Project {projectId} not found.");

        var (periodStart, periodEnd) = ComputePeriodRange(period, date);

        var tickets = await _ticketRepository.GetAppliedInRangeAsync(projectId, periodStart, periodEnd);
        var siteLogs = await _siteLogRepository.GetInRangeAsync(projectId, periodStart, periodEnd);

        return new ProjectFinanceDto
        {
            ProjectId = project.Id,
            Period = period,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            CurrentDirectExpenses = project.CurrentDirectExpenses,
            PendingExpenses = project.PendingExpenses,
            TotalHoursWorked = siteLogs.SelectMany(s => s.Workers).Sum(w => w.HoursWorked),
            Materials = tickets.Select(t => new ProjectFinanceMaterialDto
            {
                MaterialName = t.MaterialName,
                SupplierName = t.Supplier.Name,
                Quantity = t.Quantity,
                Total = t.Total,
                Date = t.CreatedAt
            }).ToList()
        };
    }

    // SUPUESTO (sección 15, el documento no define límites exactos de periodo):
    // - week: ventana de 7 días iniciando en `date` (mismo criterio WeekStart→WeekEnd usado en
    //   SiteLog/Payroll).
    // - month: del primer al último día del mes calendario que contiene `date`.
    // - year: del 1 de enero al 31 de diciembre del año que contiene `date`.
    private static (DateTime Start, DateTime End) ComputePeriodRange(FinancePeriod period, DateTime date) => period switch
    {
        FinancePeriod.Week => (date, date.AddDays(6)),
        FinancePeriod.Month => (new DateTime(date.Year, date.Month, 1), new DateTime(date.Year, date.Month, 1).AddMonths(1).AddDays(-1)),
        FinancePeriod.Year => (new DateTime(date.Year, 1, 1), new DateTime(date.Year, 12, 31)),
        _ => throw new ArgumentOutOfRangeException(nameof(period))
    };
}
```

- [ ] **Step 3: Add `CreateProjectFinanceService` to `LvTest/Common/ServiceFactory.cs`**

Add to the `using` block:
```csharp
using LvApplication.Services.Finance;
```

Add at the end of the class:
```csharp

    public static ProjectFinanceService CreateProjectFinanceService(AppDbContext context) =>
        new(
            new ProjectRepository(context),
            new MaterialTicketRepository(context),
            new SiteLogRepository(context));
```

- [ ] **Step 4: Write the failing tests**

Create `LvTest/Services/Finance/ProjectFinanceServiceTests.cs`:

```csharp
using FluentAssertions;
using LvApplication.DTOs.Inventory;
using LvApplication.DTOs.Projects;
using LvApplication.DTOs.SiteLogs;
using LvDomain.Entities.Branches;
using LvDomain.Entities.Budgets;
using LvDomain.Entities.Customers;
using LvDomain.Entities.Materials;
using LvDomain.Entities.Offers;
using LvDomain.Entities.Suppliers;
using LvDomain.Enums;
using LvInfrastructure.Persistence;
using LvTest.Common;
using Microsoft.EntityFrameworkCore;

namespace LvTest.Services.Finance;

public class ProjectFinanceServiceTests
{
    private static async Task<Customer> CreateProjectCustomerAsync(AppDbContext context)
    {
        var customer = new Customer { Name = "Project Customer", CustomerType = CustomerType.Project, Status = ActiveStatus.Active, CreatedAt = DateTime.UtcNow };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer;
    }

    private static async Task<Branch> CreateBranchAsync(AppDbContext context, int operationsDirectorId)
    {
        var branch = new Branch { Name = "Test Branch", City = "San Jose", Province = "San Jose", Status = BranchStatus.Active, BranchType = BranchType.Office, OperationsDirectorId = operationsDirectorId, CreatedAt = DateTime.UtcNow };
        context.Branches.Add(branch);
        await context.SaveChangesAsync();
        return branch;
    }

    private static async Task<Supplier> CreateSupplierAsync(AppDbContext context, string name = "Proveedor Test")
    {
        var supplier = new Supplier { Name = name, Status = ActiveStatus.Active, CreatedAt = DateTime.UtcNow };
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();
        return supplier;
    }

    private static async Task<MaterialCatalog> CreateMaterialAsync(AppDbContext context, string name = "Cemento")
    {
        var material = new MaterialCatalog { Name = name, CreatedAt = DateTime.UtcNow };
        context.MaterialCatalogs.Add(material);
        await context.SaveChangesAsync();
        return material;
    }

    private static async Task<(ProjectDto Project, int ManagerId, int ProjectAdminId)> CreateActiveProjectAsync(AppDbContext context)
    {
        var director = await TestUserFactory.CreateAsync(context, $"director-{Guid.NewGuid():N}@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var manager = await TestUserFactory.CreateAsync(context, $"gm-{Guid.NewGuid():N}@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var projectAdmin = await TestUserFactory.CreateAsync(context, $"pa-{Guid.NewGuid():N}@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);

        var budget = new Budget
        {
            CustomerId = customer.Id,
            BranchId = branch.Id,
            Name = "Edificio Test",
            Status = BudgetStatus.ClientApproved,
            UtilityPercentage = 10,
            IndirectCostsTotal = 50,
            TotalBudget = 1000,
            CreatedByUserId = manager.Id,
            CreatedAt = DateTime.UtcNow
        };
        context.Budgets.Add(budget);
        await context.SaveChangesAsync();

        var offer = new Offer
        {
            BudgetId = budget.Id,
            CustomerId = customer.Id,
            OfferNumber = $"OF-TEST-{Guid.NewGuid():N}",
            OfferType = OfferType.Turnkey,
            IssueDate = new DateTime(2026, 1, 10),
            ValidityDays = 30,
            WorkLocation = "San Jose Centro",
            WorkScope = "Construccion de edificio de 3 niveles",
            EstimatedStartDate = new DateTime(2026, 2, 1),
            EstimatedDurationWeeks = 10,
            EstimatedDeliveryDate = new DateTime(2026, 2, 1).AddDays(10 * 7),
            PaymentTerms = "50% inicio, 50% entrega",
            Warranties = "1 año estructural",
            Exclusions = "No incluye mobiliario",
            TotalProjectPrice = 100000m,
            Status = OfferStatus.ClientAccepted,
            CreatedByUserId = manager.Id,
            CreatedAt = DateTime.UtcNow
        };
        context.Offers.Add(offer);
        await context.SaveChangesAsync();

        var projectService = ServiceFactory.CreateProjectService(context);
        var project = await projectService.CreateProjectAsync(new CreateProjectDto
        {
            OfferId = offer.Id,
            BranchId = branch.Id,
            StartDate = new DateTime(2026, 3, 1)
        }, manager.Id);

        return (project, manager.Id, projectAdmin.Id);
    }

    [Fact]
    public async Task GetFinanceAsync_Week_AggregatesMaterialsAndHours()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var supplier = await CreateSupplierAsync(context, "Ferretería Central");
        var material = await CreateMaterialAsync(context, "Cemento");

        var ticketService = ServiceFactory.CreateMaterialTicketService(context);
        var ticket = await ticketService.CreateAsync(project.Id, new CreateMaterialTicketDto
        {
            SupplierId = supplier.Id,
            MaterialId = material.Id,
            Quantity = 10,
            UnitPrice = 5
        }, projectAdminId); // Total = 50
        await ticketService.ApplyAsync(ticket.Id);

        var siteLogService = ServiceFactory.CreateSiteLogService(context);
        var siteLog = await siteLogService.CreateAsync(new CreateSiteLogDto
        {
            ProjectId = project.Id,
            WeekStart = new DateTime(2026, 3, 2),
            WeekEnd = new DateTime(2026, 3, 8),
            TaskDescription = "Semana de trabajo",
            Workers = new List<SiteLogWorkerDto> { new() { WorkerId = 1, HoursWorked = 40 } }
        }, projectAdminId);
        // WorkerId=1 with no Worker row is fine here — SiteLog doesn't validate Worker existence
        // (matching the pre-existing SyncWorkers behavior, unchanged by this plan).

        var service = ServiceFactory.CreateProjectFinanceService(context);

        var finance = await service.GetFinanceAsync(project.Id, FinancePeriod.Week, new DateTime(2026, 3, 2));

        finance.PeriodStart.Should().Be(new DateTime(2026, 3, 2));
        finance.PeriodEnd.Should().Be(new DateTime(2026, 3, 8));
        finance.Materials.Should().ContainSingle(m => m.MaterialName == "Cemento" && m.SupplierName == "Ferretería Central" && m.Total == 50m);
        finance.TotalHoursWorked.Should().Be(40m);
    }

    [Fact]
    public async Task GetFinanceAsync_Month_AggregatesAcrossMultipleWeeks()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);

        var siteLogService = ServiceFactory.CreateSiteLogService(context);
        await siteLogService.CreateAsync(new CreateSiteLogDto
        {
            ProjectId = project.Id,
            WeekStart = new DateTime(2026, 3, 2),
            WeekEnd = new DateTime(2026, 3, 8),
            TaskDescription = "Semana 1",
            Workers = new List<SiteLogWorkerDto> { new() { WorkerId = 1, HoursWorked = 40 } }
        }, projectAdminId);
        await siteLogService.CreateAsync(new CreateSiteLogDto
        {
            ProjectId = project.Id,
            WeekStart = new DateTime(2026, 3, 9),
            WeekEnd = new DateTime(2026, 3, 15),
            TaskDescription = "Semana 2",
            Workers = new List<SiteLogWorkerDto> { new() { WorkerId = 1, HoursWorked = 35 } }
        }, projectAdminId);

        var service = ServiceFactory.CreateProjectFinanceService(context);

        var finance = await service.GetFinanceAsync(project.Id, FinancePeriod.Month, new DateTime(2026, 3, 15));

        finance.PeriodStart.Should().Be(new DateTime(2026, 3, 1));
        finance.PeriodEnd.Should().Be(new DateTime(2026, 3, 31));
        finance.TotalHoursWorked.Should().Be(75m);
    }

    [Fact]
    public async Task GetFinanceAsync_Year_AggregatesAcrossMultipleMonths()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);

        var siteLogService = ServiceFactory.CreateSiteLogService(context);
        await siteLogService.CreateAsync(new CreateSiteLogDto
        {
            ProjectId = project.Id,
            WeekStart = new DateTime(2026, 3, 2),
            WeekEnd = new DateTime(2026, 3, 8),
            TaskDescription = "Semana de marzo",
            Workers = new List<SiteLogWorkerDto> { new() { WorkerId = 1, HoursWorked = 40 } }
        }, projectAdminId);
        await siteLogService.CreateAsync(new CreateSiteLogDto
        {
            ProjectId = project.Id,
            WeekStart = new DateTime(2026, 9, 7),
            WeekEnd = new DateTime(2026, 9, 13),
            TaskDescription = "Semana de septiembre",
            Workers = new List<SiteLogWorkerDto> { new() { WorkerId = 1, HoursWorked = 20 } }
        }, projectAdminId);

        var service = ServiceFactory.CreateProjectFinanceService(context);

        var finance = await service.GetFinanceAsync(project.Id, FinancePeriod.Year, new DateTime(2026, 6, 1));

        finance.PeriodStart.Should().Be(new DateTime(2026, 1, 1));
        finance.PeriodEnd.Should().Be(new DateTime(2026, 12, 31));
        finance.TotalHoursWorked.Should().Be(60m);
    }
}
```

- [ ] **Step 5: Run the tests**

Run: `dotnet test LvTest/LvTest/LvTest.csproj --filter "FullyQualifiedName~ProjectFinanceServiceTests"`
Expected: all 3 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add LvTest/LvApplication/Services/Finance/ LvTest/LvTest/Common/ServiceFactory.cs LvTest/LvTest/Services/Finance/ProjectFinanceServiceTests.cs
git commit -m "feat(finance): add ProjectFinanceService with week/month/year aggregation"
```

---

### Task 9: Controllers + DI registration + full verification

**Files:**
- Create: `LvApi/Controllers/Projects/ProjectChaptersController.cs`
- Create: `LvApi/Controllers/Finance/ProjectFinanceController.cs`
- Modify: `LvApi/Extensions/ServiceCollectionExtensions.cs`

**Interfaces:**
- Consumes: `IProjectChapterService` (Task 6), `IProjectFinanceService` (Task 8).
- Produces: `GET /api/projects/{projectId}/chapters`, `PUT /api/projects/{projectId}/chapters/{chapterId}/assigned-sold-total`, `GET /api/projects/{projectId}/finance?period=week|month|year&date=YYYY-MM-DD`.

- [ ] **Step 1: Create `ProjectChaptersController.cs`**

```csharp
using LvApplication.DTOs.Projects;
using LvApplication.Services.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.Projects;

[ApiController]
[Route("api")]
[Authorize]
public class ProjectChaptersController : ControllerBase
{
    private readonly IProjectChapterService _projectChapterService;

    public ProjectChaptersController(IProjectChapterService projectChapterService)
    {
        _projectChapterService = projectChapterService;
    }

    [HttpGet("projects/{projectId:int}/chapters")]
    public async Task<ActionResult<List<ProjectChapterDto>>> GetByProject(int projectId)
    {
        var result = await _projectChapterService.GetByProjectAsync(projectId);
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector,ProjectAdmin")]
    [HttpPut("projects/{projectId:int}/chapters/{chapterId:int}/assigned-sold-total")]
    public async Task<ActionResult<ProjectChapterDto>> UpdateAssignedSoldTotal(int projectId, int chapterId, UpdateAssignedSoldTotalDto request)
    {
        var result = await _projectChapterService.UpdateAssignedSoldTotalAsync(projectId, chapterId, request.AssignedSoldTotal);
        return Ok(result);
    }
}
```

- [ ] **Step 2: Create `ProjectFinanceController.cs`**

```csharp
using LvApplication.DTOs.Finance;
using LvApplication.Services.Finance;
using LvDomain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.Finance;

[ApiController]
[Route("api")]
[Authorize]
public class ProjectFinanceController : ControllerBase
{
    private readonly IProjectFinanceService _financeService;

    public ProjectFinanceController(IProjectFinanceService financeService)
    {
        _financeService = financeService;
    }

    [HttpGet("projects/{projectId:int}/finance")]
    public async Task<ActionResult<ProjectFinanceDto>> GetFinance(int projectId, [FromQuery] FinancePeriod period, [FromQuery] DateTime date)
    {
        var result = await _financeService.GetFinanceAsync(projectId, period, date);
        return Ok(result);
    }
}
```

- [ ] **Step 3: Register everything in `ServiceCollectionExtensions.cs`**

Add to the `using` block:
```csharp
using LvApplication.Services.Finance;
using LvInfrastructure.Repositories.Projects;
```

In `AddInfrastructureServices`, add after `services.AddScoped<IProjectRepository, ProjectRepository>();`:
```csharp
        services.AddScoped<IProjectChapterRepository, ProjectChapterRepository>();
```

In `AddApplicationServices`, add after `services.AddScoped<IProjectService, ProjectService>();`:
```csharp
        services.AddScoped<IProjectChapterService, ProjectChapterService>();
        services.AddScoped<IProjectFinanceService, ProjectFinanceService>();
```

- [ ] **Step 4: Full solution build**

Run: `dotnet build LvTest/LvApi/LvApi.csproj`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 5: Full test suite run**

Run: `dotnet test LvTest/LvTest/LvTest.csproj`
Expected: all tests pass — the pre-existing 201, plus 2 new `ProjectServiceTests`, plus 2 new `ProjectChapterServiceTests`, plus 3 new `ProjectFinanceServiceTests` = 208.

- [ ] **Step 6: Commit**

```bash
git add LvTest/LvApi/Controllers/Projects/ProjectChaptersController.cs LvTest/LvApi/Controllers/Finance/ProjectFinanceController.cs LvTest/LvApi/Extensions/ServiceCollectionExtensions.cs
git commit -m "feat(chapters,finance): add controllers and wire up DI"
```

---

## Self-Review

**1. Spec coverage:**
- A.1 `ChapterId` on 4 entities + DTOs + validators + service-level "belongs to budget" validation → Tasks 1, 4, 5, 7. ✅
- A.2 `ProjectChapter` entity with all 7 calculated/stored fields + unique `(ProjectId, ChapterId)` index → Tasks 1, 2. ✅
- A.3 Auto-creation on `Project` creation with the TurnKey/Percentage documented assumption → Task 6. ✅
- A.4 Manual `AssignedSoldTotal` adjustment endpoint (3-role gate) → Tasks 6, 9. ✅
- A.5 `RecalculateActualCostAsync` (3 sources, excludes `SiteLog`, triggered from `Apply`/`MarkAsPaid`/`Approve`) → Tasks 6, 7. ✅
- A.6 `GET /chapters` query endpoint → Task 9. ✅
- Part B Finance view (real-time aggregation, no new ledger table, week/month/year) → Tasks 3, 8, 9. ✅
- No migration generated → explicitly called out in Global Constraints, never appears as a step. ✅
- Tests: `ProjectChapter` auto-creation (TurnKey proportional + Percentage zero) → Task 6. `RecalculateActualCostAsync` sums 3 sources, excludes SiteLog → Task 6 (test), Task 7 (trigger wiring that makes it pass). Chapter-membership validation rejects foreign chapters → covered implicitly by `ValidateChapterAsync` in Task 7 (no dedicated negative test was in the user's list beyond "rechaza si no" — if stronger coverage is wanted, add one `*_ChapterNotInBudget_ThrowsValidationException` test per service). Manual assignment endpoint updates `ChapterProfit` → Task 6. `GET /finance` aggregates week/month/year → Task 8 (one test per period, as requested). ✅

**2. Placeholder scan:** No `TBD`/`TODO`/"add validation"/"similar to Task N" — every step has complete, runnable code, including every edit's exact before/after text. ✅

**3. Type consistency:** `IProjectChapterRepository`/`IProjectChapterService` signatures match their implementations and every call site (`ProjectService`, `MaterialTicketService`, `PayrollService`, `IncidentService`, `ProjectChaptersController`) exactly. `ChapterId` is `int?` everywhere it appears (10 DTOs, 4 entities) — no `int` vs `int?` mismatch anywhere. `FinancePeriod` used identically in `ProjectFinanceDto`, `IProjectFinanceService`, `ProjectFinanceController`. ✅

**Explicit test-breakage risk called out per the user's request:** none expected. Every entity change is an additive nullable field; every DTO change is an additive nullable property; every service constructor change is absorbed entirely by `ServiceFactory.cs` (tests never call `new XService(...)` directly) and `ServiceCollectionExtensions.cs` (production DI, not exercised by unit tests). Task 3's and Task 4's steps each end with a full-suite run specifically to catch this early, before the deeper Task 7 retrofit compounds the risk. Confirmed during execution: 0 of the 201 pre-existing tests broke at any task boundary.

**Execution notes (discovered during implementation):**
- Task 7: `IncidentService.UpdateAsync` originally only fetched `project` late (for the `PendingExpenses` adjustment). Adding a second, earlier `var project = ...` inside the new chapter-validation block triggered `CS0136` (duplicate local name in nested/enclosing scope). Fixed by hoisting the single `project` fetch to before chapter validation and reusing it for the later `PendingExpenses` update, removing the duplicate fetch entirely (also a minor simplification of the original code).
- Task 8: `MaterialTicket.CreatedAt` is always stamped with the real `DateTime.UtcNow` at creation time (no "as of" parameter exists on `MaterialTicketService`). The `GetFinanceAsync_Week_...` test originally asserted against a fixed 2026-03 week, which never matched the ticket's real creation timestamp — fixed by backdating the stored `MaterialTicket.CreatedAt` directly in the test after creation, matching the pattern used elsewhere in this test suite for backdating fixtures.
