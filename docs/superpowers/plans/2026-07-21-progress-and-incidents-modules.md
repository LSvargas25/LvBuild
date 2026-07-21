# Progress (Avances) + Incidents (Imprevistos) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add two modules to LVConstrucciones: (A) `ProjectProgress` — an audit-only record created automatically whenever a `SiteLog` is approved, capturing the project's completion percentage; (B) `Incident` (Imprevisto) — a Draft/Approved cost-tracking aggregate (materials + worker hours) that moves money between `Project.PendingExpenses` and `Project.CurrentDirectExpenses`, following the exact same layering used by Payroll/SiteLogs/Budgets.

**Architecture:** Part A hooks into the existing `SiteLogService.ApproveAsync` (Fase 8) via a new `IProjectProgressService.CalculateAndRecordAsync(siteLogId)` — a self-contained operation that re-fetches everything it needs (SiteLog → Project → Offer) rather than receiving already-loaded entities, so it stays independently callable and does not create a circular service dependency (it depends on `ISiteLogRepository`, not `ISiteLogService`). Part B is a standard two-child-collection aggregate (`Incident` → `IncidentMaterial`/`IncidentWorker`, sibling collections, not nested), reusing the exact `PendingExpenses`-adjust-by-difference pattern already proven in `MaterialTicketService.UpdateAsync`/`ApplyAsync`.

**Tech Stack:** .NET 8, EF Core (`UseInMemoryDatabase` in tests), FluentValidation, xUnit + FluentAssertions — same as every prior module.

## Global Constraints

- Same conventions as the Payroll plan: `ImplicitUsings` enabled everywhere, validators auto-registered via assembly scan, EF configs auto-applied via `ApplyConfigurationsFromAssembly`, tests use real `AppDbContext` + `UseInMemoryDatabase` via `ServiceFactory`/`TestDbContextFactory`, no mocking framework.
- **Namespace/type-name collision rule learned from the Payroll module:** a bare type reference is only ambiguous with its own containing namespace when the *exact identifier* matches the namespace's last segment (e.g. `Payroll` class inside a file whose namespace ends in `...Payroll`). Here, `ProjectProgress` (class) vs `Progress` (namespace segment) and `Incident` (class) vs `Incidents` (namespace segment) are different identifiers — **no qualification workaround is needed in this plan**, unlike `PayrollService.cs`/`PayrollRepository.cs`.
- **Two explicitly documented assumptions** (spec sections 13/14 do not give exact formulas) — both must ship as a code comment at the exact point they're used, not just in this plan:
  1. `ProgressPercentage = MIN(100, elapsedWeeks / Offer.EstimatedDurationWeeks * 100)`, where `elapsedWeeks = (SiteLog.WeekEnd - Project.StartDate).TotalDays / 7`.
  2. `Incident.TotalCost = Σ(IncidentMaterial.Quantity * ProjectInventoryItem.ReferenceUnitCost) + Σ(IncidentWorker.HoursUsed * Worker.HourlyRate)`.
- **Explicitly out of scope per the user:** approving an `Incident` does **not** touch `ProjectInventoryItem.CurrentQuantity`. Only the financial move (`PendingExpenses` → `CurrentDirectExpenses`) happens. Do not add inventory deduction — flag it as a question in the final summary instead.
- `ProjectProgress` is create-only: no `UpdateAsync`, no `DeleteAsync`, no controller mutation endpoints — only `GET`.
- Money fields: `decimal(18,2)`. Hours/percentage fields: `decimal(5,2)`.
- Role checks on state-changing endpoints are enforced only via `[Authorize(Roles = "...")]` on the controller action (no `actingUserRoles` threaded through services), exactly like `SiteLogsController`/`PayrollsController`.
- **Do NOT generate an EF Core migration.**

---

## File Structure

| File | Responsibility |
|---|---|
| `LvDomain/Enums/IncidentStatus.cs` | `Draft`, `Approved` |
| `LvDomain/Entities/Progress/ProjectProgress.cs` | Audit-only entity |
| `LvDomain/Entities/Incidents/Incident.cs` | Aggregate root |
| `LvDomain/Entities/Incidents/IncidentMaterial.cs` | Child: material cost line |
| `LvDomain/Entities/Incidents/IncidentWorker.cs` | Child: labor cost line |
| `LvApplication/DTOs/Progress/ProjectProgressDto.cs` | Response DTO |
| `LvApplication/DTOs/Incidents/*.cs` | Input + response DTOs |
| `LvApplication/Validators/Incidents/*.cs` | FluentValidation validators |
| `LvApplication/Services/Progress/IProjectProgressRepository.cs`, `IProjectProgressService.cs`, `ProjectProgressService.cs` | Progress business logic |
| `LvApplication/Services/Incidents/IIncidentRepository.cs`, `IIncidentService.cs`, `IncidentService.cs` | Incident business logic |
| `LvApplication/Services/SiteLogs/SiteLogService.cs` | Modify: inject `IProjectProgressService`, call it from `ApproveAsync` |
| `LvInfrastructure/Persistence/Configurations/Progress/ProjectProgressConfiguration.cs` | EF config |
| `LvInfrastructure/Persistence/Configurations/Incidents/*.cs` | EF configs |
| `LvInfrastructure/Repositories/Progress/ProjectProgressRepository.cs` | EF repo |
| `LvInfrastructure/Repositories/Incidents/IncidentRepository.cs` | EF repo |
| `LvInfrastructure/Persistence/AppDbContext.cs` | Modify: add 4 `DbSet`s |
| `LvApi/Controllers/Progress/ProjectProgressController.cs` | `GET` history endpoint |
| `LvApi/Controllers/Incidents/IncidentsController.cs` | 7 endpoints |
| `LvApi/Extensions/ServiceCollectionExtensions.cs` | Modify: register 4 new repo/service pairs |
| `LvTest/Common/ServiceFactory.cs` | Modify: add `CreateProjectProgressService`, `CreateIncidentService`; update `CreateSiteLogService` |
| `LvTest/Services/SiteLogs/SiteLogServiceTests.cs` | Modify: add Progress-calculation tests |
| `LvTest/Services/Incidents/IncidentServiceTests.cs` | New tests |

---

### Task 1: Enum + domain entities

**Files:**
- Create: `LvDomain/Enums/IncidentStatus.cs`
- Create: `LvDomain/Entities/Progress/ProjectProgress.cs`
- Create: `LvDomain/Entities/Incidents/Incident.cs`
- Create: `LvDomain/Entities/Incidents/IncidentMaterial.cs`
- Create: `LvDomain/Entities/Incidents/IncidentWorker.cs`

**Interfaces:**
- Consumes: `LvDomain.Entities.Projects.Project`, `LvDomain.Entities.SiteLogs.SiteLog`, `LvDomain.Entities.Auth.User`, `LvDomain.Entities.Materials.MaterialCatalog`, `LvDomain.Entities.Workers.Worker`, `LvDomain.Common.BaseEntity` (all pre-existing).
- Produces: `IncidentStatus { Draft, Approved }`; `ProjectProgress { Id, ProjectId, Project, SiteLogId, SiteLog, ProgressPercentage, CalculatedAt }`; `Incident { Id, ProjectId, Project, Date, Description, Status, TotalCost, CreatedByUserId, CreatedByUser, ApprovedByUserId, ApprovedByUser, Materials, Workers }`; `IncidentMaterial { Id, IncidentId, Incident, MaterialId, Material, Quantity }`; `IncidentWorker { Id, IncidentId, Incident, WorkerId, Worker, HoursUsed }` — every later task depends on these exact names.

- [ ] **Step 1: Create `IncidentStatus.cs`**

```csharp
namespace LvDomain.Enums;

public enum IncidentStatus
{
    Draft,
    Approved
}
```

- [ ] **Step 2: Create `ProjectProgress.cs`**

```csharp
using LvDomain.Common;
using LvDomain.Entities.Projects;
using LvDomain.Entities.SiteLogs;

namespace LvDomain.Entities.Progress;

public class ProjectProgress : BaseEntity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int SiteLogId { get; set; }
    public SiteLog SiteLog { get; set; } = null!;

    public decimal ProgressPercentage { get; set; }
    public DateTime CalculatedAt { get; set; }
}
```

- [ ] **Step 3: Create `Incident.cs`**

```csharp
using LvDomain.Common;
using LvDomain.Entities.Auth;
using LvDomain.Entities.Projects;
using LvDomain.Enums;

namespace LvDomain.Entities.Incidents;

public class Incident : BaseEntity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public IncidentStatus Status { get; set; }
    public decimal TotalCost { get; set; }

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public int? ApprovedByUserId { get; set; }
    public User? ApprovedByUser { get; set; }

    public ICollection<IncidentMaterial> Materials { get; set; } = new List<IncidentMaterial>();
    public ICollection<IncidentWorker> Workers { get; set; } = new List<IncidentWorker>();
}
```

- [ ] **Step 4: Create `IncidentMaterial.cs`**

```csharp
using LvDomain.Common;
using LvDomain.Entities.Materials;

namespace LvDomain.Entities.Incidents;

public class IncidentMaterial : BaseEntity
{
    public int IncidentId { get; set; }
    public Incident Incident { get; set; } = null!;

    public int MaterialId { get; set; }
    public MaterialCatalog Material { get; set; } = null!;

    public decimal Quantity { get; set; }
}
```

- [ ] **Step 5: Create `IncidentWorker.cs`**

```csharp
using LvDomain.Common;
using LvDomain.Entities.Workers;

namespace LvDomain.Entities.Incidents;

public class IncidentWorker : BaseEntity
{
    public int IncidentId { get; set; }
    public Incident Incident { get; set; } = null!;

    public int WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    public decimal HoursUsed { get; set; }
}
```

- [ ] **Step 6: Build to verify it compiles**

Run: `dotnet build LvTest/LvDomain/LvDomain.csproj`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 7: Commit**

```bash
git add LvTest/LvDomain/Enums/IncidentStatus.cs LvTest/LvDomain/Entities/Progress/ LvTest/LvDomain/Entities/Incidents/
git commit -m "feat(progress,incidents): add domain entities and IncidentStatus enum"
```

---

### Task 2: EF Core configuration + DbContext registration

**Files:**
- Create: `LvInfrastructure/Persistence/Configurations/Progress/ProjectProgressConfiguration.cs`
- Create: `LvInfrastructure/Persistence/Configurations/Incidents/IncidentConfiguration.cs`
- Create: `LvInfrastructure/Persistence/Configurations/Incidents/IncidentMaterialConfiguration.cs`
- Create: `LvInfrastructure/Persistence/Configurations/Incidents/IncidentWorkerConfiguration.cs`
- Modify: `LvInfrastructure/Persistence/AppDbContext.cs`

**Interfaces:**
- Consumes: entities from Task 1.
- Produces: `AppDbContext.ProjectProgresses`, `AppDbContext.Incidents`, `AppDbContext.IncidentMaterials`, `AppDbContext.IncidentWorkers` `DbSet`s — Task 4 (repositories) depends on these.

- [ ] **Step 1: Create `ProjectProgressConfiguration.cs`**

```csharp
using LvDomain.Entities.Progress;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Progress;

public class ProjectProgressConfiguration : IEntityTypeConfiguration<ProjectProgress>
{
    public void Configure(EntityTypeBuilder<ProjectProgress> builder)
    {
        builder.ToTable("ProjectProgresses");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ProgressPercentage).HasColumnType("decimal(5,2)");

        builder.HasOne(p => p.Project)
            .WithMany()
            .HasForeignKey(p => p.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.SiteLog)
            .WithMany()
            .HasForeignKey(p => p.SiteLogId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.SiteLogId).IsUnique();
    }
}
```

- [ ] **Step 2: Create `IncidentConfiguration.cs`**

```csharp
using LvDomain.Entities.Incidents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Incidents;

public class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("Incidents");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Description).IsRequired();
        builder.Property(i => i.TotalCost).HasColumnType("decimal(18,2)");

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(i => i.Project)
            .WithMany()
            .HasForeignKey(i => i.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.CreatedByUser)
            .WithMany()
            .HasForeignKey(i => i.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.ApprovedByUser)
            .WithMany()
            .HasForeignKey(i => i.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

- [ ] **Step 3: Create `IncidentMaterialConfiguration.cs`**

```csharp
using LvDomain.Entities.Incidents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Incidents;

public class IncidentMaterialConfiguration : IEntityTypeConfiguration<IncidentMaterial>
{
    public void Configure(EntityTypeBuilder<IncidentMaterial> builder)
    {
        builder.ToTable("IncidentMaterials");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Quantity).HasColumnType("decimal(18,2)");

        builder.HasOne(m => m.Incident)
            .WithMany(i => i.Materials)
            .HasForeignKey(m => m.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(m => m.IncidentId);

        builder.HasOne(m => m.Material)
            .WithMany()
            .HasForeignKey(m => m.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(m => m.MaterialId);
    }
}
```

- [ ] **Step 4: Create `IncidentWorkerConfiguration.cs`**

```csharp
using LvDomain.Entities.Incidents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Incidents;

public class IncidentWorkerConfiguration : IEntityTypeConfiguration<IncidentWorker>
{
    public void Configure(EntityTypeBuilder<IncidentWorker> builder)
    {
        builder.ToTable("IncidentWorkers");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.HoursUsed).HasColumnType("decimal(5,2)");

        builder.HasOne(w => w.Incident)
            .WithMany(i => i.Workers)
            .HasForeignKey(w => w.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(w => w.IncidentId);

        builder.HasOne(w => w.Worker)
            .WithMany()
            .HasForeignKey(w => w.WorkerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(w => w.WorkerId);
    }
}
```

- [ ] **Step 5: Register the four `DbSet`s in `AppDbContext.cs`**

Add to the `using` block:

```csharp
using LvDomain.Entities.Incidents;
using LvDomain.Entities.Progress;
```

Add after the `Payroll` `DbSet`s (after `public DbSet<PayrollDetailPayment> PayrollDetailPayments => Set<PayrollDetailPayment>();`):

```csharp

    public DbSet<ProjectProgress> ProjectProgresses => Set<ProjectProgress>();

    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<IncidentMaterial> IncidentMaterials => Set<IncidentMaterial>();
    public DbSet<IncidentWorker> IncidentWorkers => Set<IncidentWorker>();
```

- [ ] **Step 6: Build to verify it compiles**

Run: `dotnet build LvTest/LvInfrastructure/LvInfrastructure.csproj`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 7: Commit**

```bash
git add LvTest/LvInfrastructure/Persistence/Configurations/Progress/ LvTest/LvInfrastructure/Persistence/Configurations/Incidents/ LvTest/LvInfrastructure/Persistence/AppDbContext.cs
git commit -m "feat(progress,incidents): add EF Core configuration"
```

---

### Task 3: DTOs

**Files:**
- Create: `LvApplication/DTOs/Progress/ProjectProgressDto.cs`
- Create: `LvApplication/DTOs/Incidents/IncidentMaterialDto.cs`
- Create: `LvApplication/DTOs/Incidents/IncidentWorkerDto.cs`
- Create: `LvApplication/DTOs/Incidents/CreateIncidentDto.cs`
- Create: `LvApplication/DTOs/Incidents/UpdateIncidentDto.cs`
- Create: `LvApplication/DTOs/Incidents/IncidentMaterialResponseDto.cs`
- Create: `LvApplication/DTOs/Incidents/IncidentWorkerResponseDto.cs`
- Create: `LvApplication/DTOs/Incidents/IncidentDto.cs`

**Interfaces:**
- Consumes: `LvDomain.Enums.IncidentStatus` (Task 1).
- Produces: `ProjectProgressDto { Id, ProjectId, SiteLogId, ProgressPercentage, CalculatedAt }`; `IncidentMaterialDto { MaterialId, Quantity }`; `IncidentWorkerDto { WorkerId, HoursUsed }`; `CreateIncidentDto { ProjectId, Date, Description, Materials, Workers }`; `UpdateIncidentDto { Date, Description, Materials, Workers }`; `IncidentMaterialResponseDto { Id, MaterialId, Quantity }`; `IncidentWorkerResponseDto { Id, WorkerId, HoursUsed }`; `IncidentDto { Id, ProjectId, Date, Description, Status, TotalCost, CreatedByUserId, ApprovedByUserId, Materials, Workers }` — Tasks 5, 6, 7, 8, 9 depend on these shapes.

- [ ] **Step 1: Create `ProjectProgressDto.cs`**

```csharp
namespace LvApplication.DTOs.Progress;

public class ProjectProgressDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int SiteLogId { get; set; }
    public decimal ProgressPercentage { get; set; }
    public DateTime CalculatedAt { get; set; }
}
```

- [ ] **Step 2: Create the Incident input DTOs**

`LvApplication/DTOs/Incidents/IncidentMaterialDto.cs`:
```csharp
namespace LvApplication.DTOs.Incidents;

public class IncidentMaterialDto
{
    public int MaterialId { get; set; }
    public decimal Quantity { get; set; }
}
```

`LvApplication/DTOs/Incidents/IncidentWorkerDto.cs`:
```csharp
namespace LvApplication.DTOs.Incidents;

public class IncidentWorkerDto
{
    public int WorkerId { get; set; }
    public decimal HoursUsed { get; set; }
}
```

`LvApplication/DTOs/Incidents/CreateIncidentDto.cs`:
```csharp
namespace LvApplication.DTOs.Incidents;

public class CreateIncidentDto
{
    public int ProjectId { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<IncidentMaterialDto> Materials { get; set; } = new();
    public List<IncidentWorkerDto> Workers { get; set; } = new();
}
```

`LvApplication/DTOs/Incidents/UpdateIncidentDto.cs`:
```csharp
namespace LvApplication.DTOs.Incidents;

public class UpdateIncidentDto
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<IncidentMaterialDto> Materials { get; set; } = new();
    public List<IncidentWorkerDto> Workers { get; set; } = new();
}
```

- [ ] **Step 3: Create the Incident response DTOs**

`LvApplication/DTOs/Incidents/IncidentMaterialResponseDto.cs`:
```csharp
namespace LvApplication.DTOs.Incidents;

public class IncidentMaterialResponseDto
{
    public int Id { get; set; }
    public int MaterialId { get; set; }
    public decimal Quantity { get; set; }
}
```

`LvApplication/DTOs/Incidents/IncidentWorkerResponseDto.cs`:
```csharp
namespace LvApplication.DTOs.Incidents;

public class IncidentWorkerResponseDto
{
    public int Id { get; set; }
    public int WorkerId { get; set; }
    public decimal HoursUsed { get; set; }
}
```

`LvApplication/DTOs/Incidents/IncidentDto.cs`:
```csharp
using LvDomain.Enums;

namespace LvApplication.DTOs.Incidents;

public class IncidentDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public IncidentStatus Status { get; set; }
    public decimal TotalCost { get; set; }
    public int CreatedByUserId { get; set; }
    public int? ApprovedByUserId { get; set; }
    public List<IncidentMaterialResponseDto> Materials { get; set; } = new();
    public List<IncidentWorkerResponseDto> Workers { get; set; } = new();
}
```

- [ ] **Step 4: Build to verify it compiles**

Run: `dotnet build LvTest/LvApplication/LvApplication.csproj`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 5: Commit**

```bash
git add LvTest/LvApplication/DTOs/Progress/ LvTest/LvApplication/DTOs/Incidents/
git commit -m "feat(progress,incidents): add DTOs"
```

---

### Task 4: Repositories

**Files:**
- Create: `LvApplication/Services/Progress/IProjectProgressRepository.cs`
- Create: `LvInfrastructure/Repositories/Progress/ProjectProgressRepository.cs`
- Create: `LvApplication/Services/Incidents/IIncidentRepository.cs`
- Create: `LvInfrastructure/Repositories/Incidents/IncidentRepository.cs`

**Interfaces:**
- Consumes: `ProjectProgress`, `Incident` (Task 1), `AppDbContext.ProjectProgresses`/`Incidents` (Task 2).
- Produces: `IProjectProgressRepository { AddAsync(ProjectProgress), GetPagedByProjectAsync(int,int,int) }`; `IIncidentRepository { GetByIdAsync(int), GetPagedAsync(int,int), GetPagedByProjectAsync(int,int,int), AddAsync(Incident), UpdateAsync(Incident), DeleteAsync(Incident) }` — Tasks 5, 6, 8 depend on these.

- [ ] **Step 1: Create `IProjectProgressRepository.cs`**

```csharp
using LvDomain.Entities.Progress;

namespace LvApplication.Services.Progress;

public interface IProjectProgressRepository
{
    Task AddAsync(ProjectProgress progress);
    Task<(List<ProjectProgress> Items, int TotalCount)> GetPagedByProjectAsync(int projectId, int pageNumber, int pageSize);
}
```

- [ ] **Step 2: Create `ProjectProgressRepository.cs`**

```csharp
using LvApplication.Services.Progress;
using LvDomain.Entities.Progress;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LvInfrastructure.Repositories.Progress;

public class ProjectProgressRepository : IProjectProgressRepository
{
    private readonly AppDbContext _context;

    public ProjectProgressRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ProjectProgress progress)
    {
        _context.ProjectProgresses.Add(progress);
        await _context.SaveChangesAsync();
    }

    public async Task<(List<ProjectProgress> Items, int TotalCount)> GetPagedByProjectAsync(int projectId, int pageNumber, int pageSize)
    {
        var query = _context.ProjectProgresses.Where(p => p.ProjectId == projectId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.CalculatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
```

- [ ] **Step 3: Create `IIncidentRepository.cs`**

```csharp
using LvDomain.Entities.Incidents;

namespace LvApplication.Services.Incidents;

public interface IIncidentRepository
{
    Task<Incident?> GetByIdAsync(int id);
    Task<(List<Incident> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);
    Task<(List<Incident> Items, int TotalCount)> GetPagedByProjectAsync(int projectId, int pageNumber, int pageSize);
    Task AddAsync(Incident incident);
    Task UpdateAsync(Incident incident);
    Task DeleteAsync(Incident incident);
}
```

- [ ] **Step 4: Create `IncidentRepository.cs`**

```csharp
using LvApplication.Services.Incidents;
using LvDomain.Entities.Incidents;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LvInfrastructure.Repositories.Incidents;

public class IncidentRepository : IIncidentRepository
{
    private readonly AppDbContext _context;

    public IncidentRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<Incident> IncidentsWithChildren => _context.Incidents
        .Include(i => i.Materials)
        .Include(i => i.Workers);

    public Task<Incident?> GetByIdAsync(int id) =>
        IncidentsWithChildren.FirstOrDefaultAsync(i => i.Id == id);

    public async Task<(List<Incident> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize)
    {
        var totalCount = await _context.Incidents.CountAsync();

        var items = await IncidentsWithChildren
            .OrderBy(i => i.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(List<Incident> Items, int TotalCount)> GetPagedByProjectAsync(int projectId, int pageNumber, int pageSize)
    {
        var query = _context.Incidents.Where(i => i.ProjectId == projectId);

        var totalCount = await query.CountAsync();

        var items = await IncidentsWithChildren
            .Where(i => i.ProjectId == projectId)
            .OrderBy(i => i.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddAsync(Incident incident)
    {
        _context.Incidents.Add(incident);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Incident incident)
    {
        _context.Incidents.Update(incident);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Incident incident)
    {
        _context.Incidents.Remove(incident);
        await _context.SaveChangesAsync();
    }
}
```

- [ ] **Step 5: Build to verify it compiles**

Run: `dotnet build LvTest/LvInfrastructure/LvInfrastructure.csproj`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 6: Commit**

```bash
git add LvTest/LvApplication/Services/Progress/IProjectProgressRepository.cs LvTest/LvInfrastructure/Repositories/Progress/ProjectProgressRepository.cs LvTest/LvApplication/Services/Incidents/IIncidentRepository.cs LvTest/LvInfrastructure/Repositories/Incidents/IncidentRepository.cs
git commit -m "feat(progress,incidents): add repositories"
```

---

### Task 5: ProjectProgressService + wiring into SiteLogService.ApproveAsync

**Files:**
- Create: `LvApplication/Services/Progress/IProjectProgressService.cs`
- Create: `LvApplication/Services/Progress/ProjectProgressService.cs`
- Modify: `LvApplication/Services/SiteLogs/SiteLogService.cs`
- Modify: `LvTest/Common/ServiceFactory.cs`
- Modify: `LvTest/Services/SiteLogs/SiteLogServiceTests.cs`

**Interfaces:**
- Consumes: `IProjectProgressRepository` (Task 4), `ISiteLogRepository.GetByIdAsync(int)` (pre-existing, returns `SiteLog` with `ProjectId`/`WeekEnd`), `IProjectRepository.GetByIdAsync(int)` (pre-existing, returns `Project` with `StartDate`/`OfferId`), `IOfferRepository.GetByIdAsync(int)` (pre-existing, returns `Offer` with `EstimatedDurationWeeks`).
- Produces: `IProjectProgressService { CalculateAndRecordAsync(int siteLogId) -> ProjectProgressDto, GetHistoryByProjectAsync(int,int,int) -> PagedResult<ProjectProgressDto> }` — Task 9 (controller) depends on `GetHistoryByProjectAsync`; `SiteLogService.ApproveAsync` depends on `CalculateAndRecordAsync`.

- [ ] **Step 1: Create `IProjectProgressService.cs`**

```csharp
using LvApplication.Common;
using LvApplication.DTOs.Progress;

namespace LvApplication.Services.Progress;

public interface IProjectProgressService
{
    Task<ProjectProgressDto> CalculateAndRecordAsync(int siteLogId);
    Task<PagedResult<ProjectProgressDto>> GetHistoryByProjectAsync(int projectId, int pageNumber, int pageSize);
}
```

- [ ] **Step 2: Create `ProjectProgressService.cs`**

```csharp
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
        IOfferRepository offerRepository)
    {
        _progressRepository = progressRepository;
        _siteLogRepository = siteLogRepository;
        _projectRepository = projectRepository;
        _offerRepository = offerRepository;
    }

    public async Task<ProjectProgressDto> CalculateAndRecordAsync(int siteLogId)
    {
        var siteLog = await _siteLogRepository.GetByIdAsync(siteLogId)
            ?? throw new NotFoundException($"SiteLog {siteLogId} not found.");

        var project = await _projectRepository.GetByIdAsync(siteLog.ProjectId)
            ?? throw new NotFoundException($"Project {siteLog.ProjectId} not found.");

        var offer = await _offerRepository.GetByIdAsync(project.OfferId)
            ?? throw new NotFoundException($"Offer {project.OfferId} not found.");

        // SUPUESTO (la sección 13 de la especificación no trae la fórmula exacta):
        // % de avance = semanas transcurridas entre el inicio del proyecto y el fin de
        // semana de esta bitácora, sobre la duración estimada de la oferta, con tope en 100%.
        var elapsedWeeks = (decimal)(siteLog.WeekEnd - project.StartDate).TotalDays / 7m;
        var progressPercentage = offer.EstimatedDurationWeeks > 0
            ? Math.Min(100m, elapsedWeeks / offer.EstimatedDurationWeeks * 100m)
            : 100m;

        var progress = new ProjectProgress
        {
            ProjectId = project.Id,
            SiteLogId = siteLog.Id,
            ProgressPercentage = progressPercentage,
            CalculatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await _progressRepository.AddAsync(progress);

        return MapToDto(progress);
    }

    public async Task<PagedResult<ProjectProgressDto>> GetHistoryByProjectAsync(int projectId, int pageNumber, int pageSize)
    {
        var (items, totalCount) = await _progressRepository.GetPagedByProjectAsync(projectId, pageNumber, pageSize);

        return new PagedResult<ProjectProgressDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    private static ProjectProgressDto MapToDto(ProjectProgress progress) => new()
    {
        Id = progress.Id,
        ProjectId = progress.ProjectId,
        SiteLogId = progress.SiteLogId,
        ProgressPercentage = progress.ProgressPercentage,
        CalculatedAt = progress.CalculatedAt
    };
}
```

- [ ] **Step 3: Wire `IProjectProgressService` into `SiteLogService.ApproveAsync`**

In `LvApplication/Services/SiteLogs/SiteLogService.cs`:

Add to the `using` block:
```csharp
using LvApplication.Services.Progress;
```

Add a field and constructor parameter (insert after `_inventoryRepository`):
```csharp
    private readonly IProjectInventoryItemRepository _inventoryRepository;
    private readonly IProjectProgressService _projectProgressService;
    private readonly IValidator<CreateSiteLogDto> _createValidator;
```

```csharp
    public SiteLogService(
        ISiteLogRepository siteLogRepository,
        IProjectRepository projectRepository,
        IProjectInventoryItemRepository inventoryRepository,
        IProjectProgressService projectProgressService,
        IValidator<CreateSiteLogDto> createValidator,
        IValidator<UpdateSiteLogDto> updateValidator)
    {
        _siteLogRepository = siteLogRepository;
        _projectRepository = projectRepository;
        _inventoryRepository = inventoryRepository;
        _projectProgressService = projectProgressService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }
```

In `ApproveAsync`, replace:
```csharp
        siteLog.TotalMaterials = totalMaterials;
        siteLog.Status = SiteLogStatus.Approved;
        siteLog.ApprovedByUserId = approvedByUserId;
        siteLog.UpdatedAt = DateTime.UtcNow;
        await _siteLogRepository.UpdateAsync(siteLog);

        project.TotalWorkedHours += siteLog.Workers.Sum(w => w.HoursWorked);
        project.UpdatedAt = DateTime.UtcNow;
        await _projectRepository.UpdateAsync(project);

        return MapToDto(siteLog);
```

with:
```csharp
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
```

- [ ] **Step 4: Update `LvTest/Common/ServiceFactory.cs`**

Add to the `using` block:
```csharp
using LvApplication.Services.Progress;
using LvInfrastructure.Repositories.Progress;
```

Add before `CreateSiteLogService`:
```csharp
    public static ProjectProgressService CreateProjectProgressService(AppDbContext context) =>
        new(
            new ProjectProgressRepository(context),
            new SiteLogRepository(context),
            new ProjectRepository(context),
            new OfferRepository(context));

```

Update `CreateSiteLogService` to pass the new dependency:
```csharp
    public static SiteLogService CreateSiteLogService(AppDbContext context) =>
        new(
            new SiteLogRepository(context),
            new ProjectRepository(context),
            new ProjectInventoryItemRepository(context),
            CreateProjectProgressService(context),
            new CreateSiteLogDtoValidator(),
            new UpdateSiteLogDtoValidator());
```

- [ ] **Step 5: Write the failing tests in `LvTest/Services/SiteLogs/SiteLogServiceTests.cs`**

Append these methods inside the `SiteLogServiceTests` class (before the final closing `}`):

```csharp

    [Fact]
    public async Task ApproveAsync_ZeroElapsedWeeks_SetsProgressPercentageToZero()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var service = ServiceFactory.CreateSiteLogService(context);

        var created = await service.CreateAsync(new CreateSiteLogDto
        {
            ProjectId = project.Id,
            WeekStart = project.StartDate,
            WeekEnd = project.StartDate,
            TaskDescription = "Semana inicial"
        }, projectAdminId);
        await service.SubmitToReviewAsync(created.Id);

        var approved = await service.ApproveAsync(created.Id, managerId);

        approved.ProgressPercentage.Should().Be(0m);
    }

    [Fact]
    public async Task ApproveAsync_AtMidpointOfEstimatedDuration_SetsProgressPercentageTo50()
    {
        using var context = TestDbContextFactory.Create();
        // CreateAcceptedOfferAsync sets EstimatedDurationWeeks = 10, so 5 elapsed weeks = 50%.
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var service = ServiceFactory.CreateSiteLogService(context);

        var created = await service.CreateAsync(new CreateSiteLogDto
        {
            ProjectId = project.Id,
            WeekStart = project.StartDate,
            WeekEnd = project.StartDate.AddDays(35),
            TaskDescription = "Semana intermedia"
        }, projectAdminId);
        await service.SubmitToReviewAsync(created.Id);

        var approved = await service.ApproveAsync(created.Id, managerId);

        approved.ProgressPercentage.Should().Be(50m);
    }

    [Fact]
    public async Task ApproveAsync_BeyondEstimatedDuration_CapsProgressPercentageAt100()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var service = ServiceFactory.CreateSiteLogService(context);

        var created = await service.CreateAsync(new CreateSiteLogDto
        {
            ProjectId = project.Id,
            WeekStart = project.StartDate,
            WeekEnd = project.StartDate.AddDays(140), // 20 weeks, double the 10-week estimate
            TaskDescription = "Semana muy avanzada"
        }, projectAdminId);
        await service.SubmitToReviewAsync(created.Id);

        var approved = await service.ApproveAsync(created.Id, managerId);

        approved.ProgressPercentage.Should().Be(100m);
    }

    [Fact]
    public async Task ApproveAsync_CreatesProjectProgressRecord()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var service = ServiceFactory.CreateSiteLogService(context);

        var created = await service.CreateAsync(new CreateSiteLogDto
        {
            ProjectId = project.Id,
            WeekStart = project.StartDate,
            WeekEnd = project.StartDate.AddDays(35),
            TaskDescription = "Semana intermedia"
        }, projectAdminId);
        await service.SubmitToReviewAsync(created.Id);

        await service.ApproveAsync(created.Id, managerId);

        var progressRecords = await context.ProjectProgresses.Where(p => p.SiteLogId == created.Id).ToListAsync();
        progressRecords.Should().ContainSingle();
        progressRecords[0].ProjectId.Should().Be(project.Id);
        progressRecords[0].ProgressPercentage.Should().Be(50m);
    }
```

- [ ] **Step 6: Run the tests**

Run: `dotnet test LvTest/LvTest/LvTest.csproj --filter "FullyQualifiedName~SiteLogServiceTests"`
Expected: all tests in `SiteLogServiceTests` PASS, including the 4 new ones — and every pre-existing `SiteLogServiceTests`/`PayrollServiceTests` test still passes (both use `ServiceFactory.CreateSiteLogService`, now with one extra dependency wired in).

- [ ] **Step 7: Commit**

```bash
git add LvTest/LvApplication/Services/Progress/IProjectProgressService.cs LvTest/LvApplication/Services/Progress/ProjectProgressService.cs LvTest/LvApplication/Services/SiteLogs/SiteLogService.cs LvTest/LvTest/Common/ServiceFactory.cs LvTest/LvTest/Services/SiteLogs/SiteLogServiceTests.cs
git commit -m "feat(progress): calculate and record ProjectProgress on SiteLog approval"
```

---

### Task 6: Incident validators

**Files:**
- Create: `LvApplication/Validators/Incidents/CreateIncidentDtoValidator.cs`
- Create: `LvApplication/Validators/Incidents/UpdateIncidentDtoValidator.cs`

**Interfaces:**
- Consumes: `CreateIncidentDto`, `UpdateIncidentDto` (Task 3).
- Produces: `IValidator<CreateIncidentDto>`, `IValidator<UpdateIncidentDto>` — Task 8 (service) and Task 9 (tests) depend on these.

- [ ] **Step 1: Create `CreateIncidentDtoValidator.cs`**

```csharp
using FluentValidation;
using LvApplication.DTOs.Incidents;

namespace LvApplication.Validators.Incidents;

public class CreateIncidentDtoValidator : AbstractValidator<CreateIncidentDto>
{
    public CreateIncidentDtoValidator()
    {
        RuleFor(x => x.ProjectId).GreaterThan(0);
        RuleFor(x => x.Description).NotEmpty();

        RuleForEach(x => x.Materials).ChildRules(material =>
        {
            material.RuleFor(m => m.MaterialId).GreaterThan(0);
            material.RuleFor(m => m.Quantity).GreaterThan(0);
        });

        RuleForEach(x => x.Workers).ChildRules(worker =>
        {
            worker.RuleFor(w => w.WorkerId).GreaterThan(0);
            worker.RuleFor(w => w.HoursUsed).GreaterThan(0);
        });
    }
}
```

- [ ] **Step 2: Create `UpdateIncidentDtoValidator.cs`**

```csharp
using FluentValidation;
using LvApplication.DTOs.Incidents;

namespace LvApplication.Validators.Incidents;

public class UpdateIncidentDtoValidator : AbstractValidator<UpdateIncidentDto>
{
    public UpdateIncidentDtoValidator()
    {
        RuleFor(x => x.Description).NotEmpty();

        RuleForEach(x => x.Materials).ChildRules(material =>
        {
            material.RuleFor(m => m.MaterialId).GreaterThan(0);
            material.RuleFor(m => m.Quantity).GreaterThan(0);
        });

        RuleForEach(x => x.Workers).ChildRules(worker =>
        {
            worker.RuleFor(w => w.WorkerId).GreaterThan(0);
            worker.RuleFor(w => w.HoursUsed).GreaterThan(0);
        });
    }
}
```

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build LvTest/LvApplication/LvApplication.csproj`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add LvTest/LvApplication/Validators/Incidents/
git commit -m "feat(incidents): add validators"
```

---

### Task 7: IncidentService

**Files:**
- Create: `LvApplication/Services/Incidents/IIncidentService.cs`
- Create: `LvApplication/Services/Incidents/IncidentService.cs`
- Modify: `LvTest/Common/ServiceFactory.cs`
- Test: `LvTest/Services/Incidents/IncidentServiceTests.cs`

**Interfaces:**
- Consumes: `IIncidentRepository` (Task 4), `IValidator<CreateIncidentDto>`/`IValidator<UpdateIncidentDto>` (Task 6), `IProjectRepository.GetByIdAsync(int)`/`UpdateAsync(Project)` (pre-existing), `IProjectInventoryItemRepository.GetByProjectAndMaterialAsync(int,int)` (pre-existing), `IWorkerRepository.GetByIdAsync(int)` (pre-existing).
- Produces: `IIncidentService { CreateAsync, UpdateAsync, ApproveAsync, DeleteAsync, GetByIdAsync, GetAllAsync, GetAllByProjectAsync }` — Task 9 (controller) depends on this.

- [ ] **Step 1: Create `IIncidentService.cs`**

```csharp
using LvApplication.Common;
using LvApplication.DTOs.Incidents;

namespace LvApplication.Services.Incidents;

public interface IIncidentService
{
    Task<IncidentDto> CreateAsync(CreateIncidentDto request, int createdByUserId);
    Task<IncidentDto> UpdateAsync(int id, UpdateIncidentDto request);
    Task<IncidentDto> ApproveAsync(int id, int approvedByUserId);
    Task DeleteAsync(int id);
    Task<IncidentDto> GetByIdAsync(int id);
    Task<PagedResult<IncidentDto>> GetAllAsync(int pageNumber, int pageSize);
    Task<PagedResult<IncidentDto>> GetAllByProjectAsync(int projectId, int pageNumber, int pageSize);
}
```

- [ ] **Step 2: Create the full `IncidentService.cs`**

```csharp
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
```

- [ ] **Step 3: Add `CreateIncidentService` to `LvTest/Common/ServiceFactory.cs`**

Add to the `using` block:
```csharp
using LvApplication.Services.Incidents;
using LvApplication.Validators.Incidents;
using LvInfrastructure.Repositories.Incidents;
```

Add at the end of the `ServiceFactory` class:
```csharp

    public static IncidentService CreateIncidentService(AppDbContext context) =>
        new(
            new IncidentRepository(context),
            new ProjectRepository(context),
            new ProjectInventoryItemRepository(context),
            new WorkerRepository(context),
            new CreateIncidentDtoValidator(),
            new UpdateIncidentDtoValidator());
```

- [ ] **Step 4: Write the failing tests**

Create `LvTest/Services/Incidents/IncidentServiceTests.cs`:

```csharp
using FluentAssertions;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Incidents;
using LvApplication.DTOs.Projects;
using LvDomain.Entities.Branches;
using LvDomain.Entities.Budgets;
using LvDomain.Entities.Customers;
using LvDomain.Entities.Inventory;
using LvDomain.Entities.Materials;
using LvDomain.Entities.Offers;
using LvDomain.Entities.Workers;
using LvDomain.Enums;
using LvInfrastructure.Persistence;
using LvTest.Common;
using Microsoft.EntityFrameworkCore;

namespace LvTest.Services.Incidents;

public class IncidentServiceTests
{
    private static async Task<Customer> CreateProjectCustomerAsync(AppDbContext context)
    {
        var customer = new Customer
        {
            Name = "Project Customer",
            CustomerType = CustomerType.Project,
            Status = ActiveStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer;
    }

    private static async Task<Branch> CreateBranchAsync(AppDbContext context, int operationsDirectorId)
    {
        var branch = new Branch
        {
            Name = "Test Branch",
            City = "San Jose",
            Province = "San Jose",
            Status = BranchStatus.Active,
            BranchType = BranchType.Office,
            OperationsDirectorId = operationsDirectorId,
            CreatedAt = DateTime.UtcNow
        };
        context.Branches.Add(branch);
        await context.SaveChangesAsync();
        return branch;
    }

    private static async Task<Budget> CreateApprovedBudgetAsync(AppDbContext context, int customerId, int branchId, int createdByUserId)
    {
        var budget = new Budget
        {
            CustomerId = customerId,
            BranchId = branchId,
            Name = "Edificio Test",
            Status = BudgetStatus.ClientApproved,
            UtilityPercentage = 10,
            IndirectCostsTotal = 50,
            TotalBudget = 1000,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };
        context.Budgets.Add(budget);
        await context.SaveChangesAsync();
        return budget;
    }

    private static async Task<Offer> CreateAcceptedOfferAsync(AppDbContext context, int budgetId, int customerId, int createdByUserId)
    {
        var offer = new Offer
        {
            BudgetId = budgetId,
            CustomerId = customerId,
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
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };
        context.Offers.Add(offer);
        await context.SaveChangesAsync();
        return offer;
    }

    private static async Task<MaterialCatalog> CreateMaterialAsync(AppDbContext context, string name = "Cemento")
    {
        var material = new MaterialCatalog { Name = name, CreatedAt = DateTime.UtcNow };
        context.MaterialCatalogs.Add(material);
        await context.SaveChangesAsync();
        return material;
    }

    private static async Task<Worker> CreateWorkerAsync(AppDbContext context, string name = "Trabajador Test", decimal hourlyRate = 5m)
    {
        var worker = new Worker
        {
            Name = name,
            Status = ActiveStatus.Active,
            Category = WorkerCategory.Construction,
            Type = WorkerType.Laborer,
            HourlyRate = hourlyRate,
            CreatedAt = DateTime.UtcNow
        };
        context.Workers.Add(worker);
        await context.SaveChangesAsync();
        return worker;
    }

    private static async Task<ProjectInventoryItem> CreateInventoryItemAsync(AppDbContext context, int projectId, int materialId, decimal currentQuantity, decimal referenceUnitCost)
    {
        var item = new ProjectInventoryItem
        {
            ProjectId = projectId,
            MaterialId = materialId,
            CurrentQuantity = currentQuantity,
            ReferenceUnitCost = referenceUnitCost,
            CreatedAt = DateTime.UtcNow
        };
        context.ProjectInventoryItems.Add(item);
        await context.SaveChangesAsync();
        return item;
    }

    private static async Task<(ProjectDto Project, int ManagerUserId, int ProjectAdminUserId)> CreateActiveProjectAsync(AppDbContext context)
    {
        var director = await TestUserFactory.CreateAsync(context, $"director-{Guid.NewGuid():N}@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var manager = await TestUserFactory.CreateAsync(context, $"gm-{Guid.NewGuid():N}@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var projectAdmin = await TestUserFactory.CreateAsync(context, $"pa-{Guid.NewGuid():N}@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateApprovedBudgetAsync(context, customer.Id, branch.Id, manager.Id);
        var offer = await CreateAcceptedOfferAsync(context, budget.Id, customer.Id, manager.Id);

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
    public async Task CreateAsync_CalculatesTotalCost_AndAddsToPendingExpenses()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var material = await CreateMaterialAsync(context);
        await CreateInventoryItemAsync(context, project.Id, material.Id, currentQuantity: 50, referenceUnitCost: 10);
        var worker = await CreateWorkerAsync(context, hourlyRate: 8);
        var service = ServiceFactory.CreateIncidentService(context);

        var storedProjectBefore = await context.Projects.FindAsync(project.Id);
        var pendingBefore = storedProjectBefore!.PendingExpenses;

        var result = await service.CreateAsync(new CreateIncidentDto
        {
            ProjectId = project.Id,
            Date = new DateTime(2026, 3, 10),
            Description = "Reparación de tubería",
            Materials = new List<IncidentMaterialDto> { new() { MaterialId = material.Id, Quantity = 4 } }, // 4 * 10 = 40
            Workers = new List<IncidentWorkerDto> { new() { WorkerId = worker.Id, HoursUsed = 6 } } // 6 * 8 = 48
        }, projectAdminId);

        result.Status.Should().Be(IncidentStatus.Draft);
        result.TotalCost.Should().Be(88m);

        var storedProjectAfter = await context.Projects.FindAsync(project.Id);
        storedProjectAfter!.PendingExpenses.Should().Be(pendingBefore + 88m);
    }

    [Fact]
    public async Task UpdateAsync_InDraft_AdjustsPendingExpensesByDifference()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var material = await CreateMaterialAsync(context);
        await CreateInventoryItemAsync(context, project.Id, material.Id, currentQuantity: 50, referenceUnitCost: 10);
        var service = ServiceFactory.CreateIncidentService(context);

        var created = await service.CreateAsync(new CreateIncidentDto
        {
            ProjectId = project.Id,
            Date = new DateTime(2026, 3, 10),
            Description = "Reparación de tubería",
            Materials = new List<IncidentMaterialDto> { new() { MaterialId = material.Id, Quantity = 4 } }, // 40
            Workers = new List<IncidentWorkerDto>()
        }, projectAdminId);

        var pendingAfterCreate = (await context.Projects.FindAsync(project.Id))!.PendingExpenses;

        var updated = await service.UpdateAsync(created.Id, new UpdateIncidentDto
        {
            Date = new DateTime(2026, 3, 11),
            Description = "Reparación de tubería (actualizado)",
            Materials = new List<IncidentMaterialDto> { new() { MaterialId = material.Id, Quantity = 9 } }, // 90
            Workers = new List<IncidentWorkerDto>()
        });

        updated.TotalCost.Should().Be(90m);

        var pendingAfterUpdate = (await context.Projects.FindAsync(project.Id))!.PendingExpenses;
        pendingAfterUpdate.Should().Be(pendingAfterCreate + (90m - 40m));
    }

    [Fact]
    public async Task UpdateAsync_Approved_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var material = await CreateMaterialAsync(context);
        await CreateInventoryItemAsync(context, project.Id, material.Id, currentQuantity: 50, referenceUnitCost: 10);
        var service = ServiceFactory.CreateIncidentService(context);

        var created = await service.CreateAsync(new CreateIncidentDto
        {
            ProjectId = project.Id,
            Date = new DateTime(2026, 3, 10),
            Description = "Reparación de tubería",
            Materials = new List<IncidentMaterialDto> { new() { MaterialId = material.Id, Quantity = 4 } },
            Workers = new List<IncidentWorkerDto>()
        }, projectAdminId);
        await service.ApproveAsync(created.Id, managerId);

        var act = async () => await service.UpdateAsync(created.Id, new UpdateIncidentDto
        {
            Date = new DateTime(2026, 3, 11),
            Description = "No debería aplicar",
            Materials = new List<IncidentMaterialDto>(),
            Workers = new List<IncidentWorkerDto>()
        });

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task ApproveAsync_MovesPendingToDirectExpenses()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var material = await CreateMaterialAsync(context);
        await CreateInventoryItemAsync(context, project.Id, material.Id, currentQuantity: 50, referenceUnitCost: 10);
        var service = ServiceFactory.CreateIncidentService(context);

        var created = await service.CreateAsync(new CreateIncidentDto
        {
            ProjectId = project.Id,
            Date = new DateTime(2026, 3, 10),
            Description = "Reparación de tubería",
            Materials = new List<IncidentMaterialDto> { new() { MaterialId = material.Id, Quantity = 4 } }, // 40
            Workers = new List<IncidentWorkerDto>()
        }, projectAdminId);

        var pendingBeforeApprove = (await context.Projects.FindAsync(project.Id))!.PendingExpenses;
        var directBeforeApprove = (await context.Projects.FindAsync(project.Id))!.CurrentDirectExpenses;

        var approved = await service.ApproveAsync(created.Id, managerId);

        approved.Status.Should().Be(IncidentStatus.Approved);
        approved.ApprovedByUserId.Should().Be(managerId);

        var storedProject = await context.Projects.FindAsync(project.Id);
        storedProject!.PendingExpenses.Should().Be(pendingBeforeApprove - 40m);
        storedProject.CurrentDirectExpenses.Should().Be(directBeforeApprove + 40m);

        var inventoryItem = await context.ProjectInventoryItems.FirstAsync(i => i.ProjectId == project.Id && i.MaterialId == material.Id);
        inventoryItem.CurrentQuantity.Should().Be(50m); // untouched — approving an Incident does not deduct inventory
    }

    [Fact]
    public async Task ApproveAsync_AlreadyApproved_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var material = await CreateMaterialAsync(context);
        await CreateInventoryItemAsync(context, project.Id, material.Id, currentQuantity: 50, referenceUnitCost: 10);
        var service = ServiceFactory.CreateIncidentService(context);

        var created = await service.CreateAsync(new CreateIncidentDto
        {
            ProjectId = project.Id,
            Date = new DateTime(2026, 3, 10),
            Description = "Reparación de tubería",
            Materials = new List<IncidentMaterialDto> { new() { MaterialId = material.Id, Quantity = 4 } },
            Workers = new List<IncidentWorkerDto>()
        }, projectAdminId);
        await service.ApproveAsync(created.Id, managerId);

        var act = async () => await service.ApproveAsync(created.Id, managerId);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task DeleteAsync_Draft_Succeeds_SubtractsPendingExpenses()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var material = await CreateMaterialAsync(context);
        await CreateInventoryItemAsync(context, project.Id, material.Id, currentQuantity: 50, referenceUnitCost: 10);
        var service = ServiceFactory.CreateIncidentService(context);

        var created = await service.CreateAsync(new CreateIncidentDto
        {
            ProjectId = project.Id,
            Date = new DateTime(2026, 3, 10),
            Description = "Reparación de tubería",
            Materials = new List<IncidentMaterialDto> { new() { MaterialId = material.Id, Quantity = 4 } }, // 40
            Workers = new List<IncidentWorkerDto>()
        }, projectAdminId);

        var pendingBeforeDelete = (await context.Projects.FindAsync(project.Id))!.PendingExpenses;

        await service.DeleteAsync(created.Id);

        var stored = await context.Incidents.FindAsync(created.Id);
        stored.Should().BeNull();

        var storedProject = await context.Projects.FindAsync(project.Id);
        storedProject!.PendingExpenses.Should().Be(pendingBeforeDelete - 40m);
    }

    [Fact]
    public async Task DeleteAsync_Approved_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var material = await CreateMaterialAsync(context);
        await CreateInventoryItemAsync(context, project.Id, material.Id, currentQuantity: 50, referenceUnitCost: 10);
        var service = ServiceFactory.CreateIncidentService(context);

        var created = await service.CreateAsync(new CreateIncidentDto
        {
            ProjectId = project.Id,
            Date = new DateTime(2026, 3, 10),
            Description = "Reparación de tubería",
            Materials = new List<IncidentMaterialDto> { new() { MaterialId = material.Id, Quantity = 4 } },
            Workers = new List<IncidentWorkerDto>()
        }, projectAdminId);
        await service.ApproveAsync(created.Id, managerId);

        var act = async () => await service.DeleteAsync(created.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }
}
```

- [ ] **Step 5: Run the tests**

Run: `dotnet test LvTest/LvTest/LvTest.csproj --filter "FullyQualifiedName~IncidentServiceTests"`
Expected: all 7 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add LvTest/LvApplication/Services/Incidents/IIncidentService.cs LvTest/LvApplication/Services/Incidents/IncidentService.cs LvTest/LvTest/Common/ServiceFactory.cs LvTest/LvTest/Services/Incidents/IncidentServiceTests.cs
git commit -m "feat(incidents): add IncidentService with Create/Update/Approve/Delete business rules"
```

---

### Task 8: Controllers + DI registration

**Files:**
- Create: `LvApi/Controllers/Progress/ProjectProgressController.cs`
- Create: `LvApi/Controllers/Incidents/IncidentsController.cs`
- Modify: `LvApi/Extensions/ServiceCollectionExtensions.cs`

**Interfaces:**
- Consumes: `IProjectProgressService` (Task 5), `IIncidentService` (Task 7).
- Produces: `GET /api/projects/{projectId}/progress-history`; `POST /api/incidents`, `PUT /api/incidents/{id}`, `POST /api/incidents/{id}/approve`, `DELETE /api/incidents/{id}`, `GET /api/incidents`, `GET /api/incidents/{id}`, `GET /api/projects/{projectId}/incidents`.

- [ ] **Step 1: Create `ProjectProgressController.cs`**

```csharp
using LvApplication.Common;
using LvApplication.DTOs.Progress;
using LvApplication.Services.Progress;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.Progress;

[ApiController]
[Route("api")]
[Authorize]
public class ProjectProgressController : ControllerBase
{
    private readonly IProjectProgressService _projectProgressService;

    public ProjectProgressController(IProjectProgressService projectProgressService)
    {
        _projectProgressService = projectProgressService;
    }

    [HttpGet("projects/{projectId:int}/progress-history")]
    public async Task<ActionResult<PagedResult<ProjectProgressDto>>> GetHistory(
        int projectId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _projectProgressService.GetHistoryByProjectAsync(projectId, pageNumber, pageSize);
        return Ok(result);
    }
}
```

- [ ] **Step 2: Create `IncidentsController.cs`**

```csharp
using System.Security.Claims;
using LvApplication.Common;
using LvApplication.DTOs.Incidents;
using LvApplication.Services.Incidents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.Incidents;

[ApiController]
[Route("api")]
[Authorize]
public class IncidentsController : ControllerBase
{
    private readonly IIncidentService _incidentService;

    public IncidentsController(IIncidentService incidentService)
    {
        _incidentService = incidentService;
    }

    [Authorize(Roles = "ProjectAdmin")]
    [HttpPost("incidents")]
    public async Task<ActionResult<IncidentDto>> Create(CreateIncidentDto request)
    {
        var result = await _incidentService.CreateAsync(request, GetCurrentUserId());
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [Authorize(Roles = "ProjectAdmin")]
    [HttpPut("incidents/{id:int}")]
    public async Task<ActionResult<IncidentDto>> Update(int id, UpdateIncidentDto request)
    {
        var result = await _incidentService.UpdateAsync(id, request);
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpPost("incidents/{id:int}/approve")]
    public async Task<ActionResult<IncidentDto>> Approve(int id)
    {
        var result = await _incidentService.ApproveAsync(id, GetCurrentUserId());
        return Ok(result);
    }

    [Authorize(Roles = "ProjectAdmin")]
    [HttpDelete("incidents/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _incidentService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("incidents")]
    public async Task<ActionResult<PagedResult<IncidentDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _incidentService.GetAllAsync(pageNumber, pageSize);
        return Ok(result);
    }

    [HttpGet("incidents/{id:int}")]
    public async Task<ActionResult<IncidentDto>> GetById(int id)
    {
        var result = await _incidentService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpGet("projects/{projectId:int}/incidents")]
    public async Task<ActionResult<PagedResult<IncidentDto>>> GetAllByProject(
        int projectId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _incidentService.GetAllByProjectAsync(projectId, pageNumber, pageSize);
        return Ok(result);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        return int.Parse(userIdClaim!.Value);
    }
}
```

- [ ] **Step 3: Register everything in `ServiceCollectionExtensions.cs`**

Add to the `using` block:
```csharp
using LvApplication.Services.Incidents;
using LvApplication.Services.Progress;
using LvInfrastructure.Repositories.Incidents;
using LvInfrastructure.Repositories.Progress;
```

In `AddInfrastructureServices`, add after `services.AddScoped<IPayrollRepository, PayrollRepository>();`:
```csharp
        services.AddScoped<IProjectProgressRepository, ProjectProgressRepository>();
        services.AddScoped<IIncidentRepository, IncidentRepository>();
```

In `AddApplicationServices`, add after `services.AddScoped<IPayrollService, PayrollService>();`:
```csharp
        services.AddScoped<IProjectProgressService, ProjectProgressService>();
        services.AddScoped<IIncidentService, IncidentService>();
```

- [ ] **Step 4: Full solution build**

Run: `dotnet build LvTest/LvApi/LvApi.csproj` (pulls in every dependent project)
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 5: Full test suite run**

Run: `dotnet test LvTest/LvTest/LvTest.csproj`
Expected: all tests pass — previous 190 plus the 4 new `SiteLogServiceTests` plus the 7 new `IncidentServiceTests` = 201.

- [ ] **Step 6: Commit**

```bash
git add LvTest/LvApi/Controllers/Progress/ProjectProgressController.cs LvTest/LvApi/Controllers/Incidents/IncidentsController.cs LvTest/LvApi/Extensions/ServiceCollectionExtensions.cs
git commit -m "feat(progress,incidents): add controllers and wire up DI"
```

---

## Self-Review

**1. Spec coverage:**
- `ProjectProgress` entity with documented formula assumption → Task 1, 5. ✅
- `SiteLog.ApproveAsync` hook via `IProjectProgressService.CalculateAndRecordAsync` → Task 5. ✅
- `GET /api/projects/{projectId}/progress-history` → Task 8. ✅
- No Update/Delete for `ProjectProgress` → never implemented anywhere in this plan. ✅
- `Incident`/`IncidentMaterial`/`IncidentWorker` entities, `IncidentStatus` enum → Task 1. ✅
- `TotalCost` formula (documented assumption) → Task 7. ✅
- **Inventory NOT deducted on Incident approval** → Task 7's `ApproveAsync` only touches `PendingExpenses`/`CurrentDirectExpenses`, never `ProjectInventoryItem.CurrentQuantity` — verified by the `ApproveAsync_MovesPendingToDirectExpenses` test asserting `CurrentQuantity` stays at 50. ✅
- `CreateAsync`/`UpdateAsync`/`ApproveAsync`/`DeleteAsync` business rules incl. role gates on the controller → Task 7 (service) + Task 8 (controller). ✅
- DTOs, Validators, Repository, Service, Controller, EF config → Tasks 3, 4, 6, 7, 8. ✅
- No migration generated → explicitly called out in Global Constraints. ✅
- Tests: all bullet points from the user's Task 8 list are covered 1:1 (`ApproveAsync_ZeroElapsedWeeks_...`, `ApproveAsync_AtMidpointOfEstimatedDuration_...`, `ApproveAsync_BeyondEstimatedDuration_CapsAt100`, `ApproveAsync_CreatesProjectProgressRecord`, `CreateAsync_CalculatesTotalCost_AndAddsToPendingExpenses`, `UpdateAsync_InDraft_AdjustsPendingExpensesByDifference`/`UpdateAsync_Approved_Throws...`, `ApproveAsync_MovesPendingToDirectExpenses`/`ApproveAsync_AlreadyApproved_Throws...`, `DeleteAsync_Draft_Succeeds...`/`DeleteAsync_Approved_Throws...`). ✅

**2. Placeholder scan:** No `TBD`/`TODO`/"add validation"/"similar to Task N" — every step has complete, runnable code. ✅

**3. Type consistency:** `IProjectProgressRepository`/`IIncidentRepository` signatures match their implementations exactly; `SiteLogService`'s new constructor parameter order matches the `ServiceFactory.CreateSiteLogService` update; `IncidentDto`/`ProjectProgressDto` property names match `MapToDto` in both services. Cross-checked `IncidentMaterial.MaterialId`/`Quantity`, `IncidentWorker.WorkerId`/`HoursUsed` used identically in entity, config, service, DTO, and tests. ✅

**Open question for the user (per explicit instruction not to decide this unilaterally):** should approving an `Incident` also deduct `ProjectInventoryItem.CurrentQuantity` for each `IncidentMaterial`, the same way `SiteLog.ApproveAsync` does? The spec (section 14) only mentions the financial move (`GastosPendientes` → `GastosDirectosActuales`) and says nothing about inventory. This plan deliberately does **not** deduct inventory, per the user's explicit instruction — flagged here for their decision, not decided by the plan.
