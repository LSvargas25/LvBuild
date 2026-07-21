# Payroll Module Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a complete "Payroll" module (Planilla) to LVConstrucciones — entities, DTOs, validators, repository, service, controller, EF configuration, and tests — following the exact Clean Architecture layering already used by the SiteLogs and Budgets modules.

**Architecture:** One aggregate root (`Payroll`) with two levels of owned children (`PayrollDetail` → `PayrollDetailPayment`), created only from an `Approved` SiteLog (1:1 via a unique `SiteLogId` FK) and mirroring the full-aggregate-replace pattern already used by `BudgetService`/`SiteLogService`. `MarkAsPaidAsync` is the single moment three other subsystems get touched: `Project.CurrentDirectExpenses`, `SiteLog.TotalPayroll` (via the `ISiteLogService.UpdateTotalPayrollAsync` hook already present in the codebase), and `Project.WeeksCounter` (via the `IProjectService.IncrementWeekCounterAsync` hook already present in the codebase).

**Tech Stack:** .NET 8, EF Core (SQL Server in production / `UseInMemoryDatabase` in tests), FluentValidation, xUnit + FluentAssertions.

## Global Constraints

- Target framework: .NET 8, `ImplicitUsings` enabled in every project (no need to add `using System.Linq;` etc.).
- Follow the existing layering exactly: `LvDomain` (entities/enums) → `LvApplication` (DTOs/validators/service interfaces+impls) → `LvInfrastructure` (repositories/EF configs) → `LvApi` (controllers/DI wiring). Test project is `LvTest`.
- Module folder name is **`Payroll`** (singular) in every layer, as already scaffolded (`.gitkeep` placeholders already exist in each layer's `Payroll` folder — just add files next to them).
- Validators are auto-registered via `services.AddValidatorsFromAssembly(applicationAssembly)` in `LvApi/Extensions/ServiceCollectionExtensions.cs` — never register a validator manually.
- EF configurations are auto-applied via `modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly)` in `AppDbContext.OnModelCreating` — never call `.ApplyConfiguration()` manually.
- Tests use the real `AppDbContext` against `UseInMemoryDatabase` (see `LvTest/Common/TestDbContextFactory.cs`) — there is no mocking framework in this codebase; services are constructed directly via `LvTest/Common/ServiceFactory.cs`.
- **Do NOT generate an EF Core migration** — the user explicitly asked for this to be deferred.
- Role checks for state-transition endpoints are enforced only via `[Authorize(Roles = "...")]` on the controller action, exactly like `SiteLogsController` — do not thread `actingUserRoles` through the service for these.
- Money fields: `decimal(18,2)`. Hours fields: `decimal(5,2)`. Enums are stored as strings (`HasConversion<string>().HasMaxLength(...)`).

---

## File Structure

| File | Responsibility |
|---|---|
| `LvDomain/Enums/PayrollStatus.cs` | `Pending`, `Paid` |
| `LvDomain/Enums/PayrollPaymentType.cs` | `Full`, `Advance`, `Vacation`, `Overtime` |
| `LvDomain/Enums/PaymentMethod.cs` | `Transfer`, `Cash` |
| `LvDomain/Entities/Payroll/Payroll.cs` | Aggregate root entity |
| `LvDomain/Entities/Payroll/PayrollDetail.cs` | Per-worker/day line entity |
| `LvDomain/Entities/Payroll/PayrollDetailPayment.cs` | Split-payment entity |
| `LvApplication/DTOs/Payroll/*.cs` | Input + response DTOs |
| `LvApplication/Validators/Payroll/*.cs` | FluentValidation validators |
| `LvApplication/Services/Payroll/IPayrollRepository.cs` | Repository interface |
| `LvApplication/Services/Payroll/IPayrollService.cs` | Service interface |
| `LvApplication/Services/Payroll/PayrollService.cs` | Business logic |
| `LvInfrastructure/Repositories/Payroll/PayrollRepository.cs` | EF Core repository |
| `LvInfrastructure/Persistence/Configurations/Payroll/*.cs` | EF Core entity configs |
| `LvInfrastructure/Persistence/AppDbContext.cs` | Modify: add 3 `DbSet`s |
| `LvApi/Controllers/Payroll/PayrollsController.cs` | REST endpoints |
| `LvApi/Extensions/ServiceCollectionExtensions.cs` | Modify: register repo + service |
| `LvTest/Common/ServiceFactory.cs` | Modify: add `CreatePayrollService` |
| `LvTest/Services/Payroll/PayrollServiceTests.cs` | Service tests |

---

### Task 1: Enums

**Files:**
- Create: `LvDomain/Enums/PayrollStatus.cs`
- Create: `LvDomain/Enums/PayrollPaymentType.cs`
- Create: `LvDomain/Enums/PaymentMethod.cs`

**Interfaces:**
- Produces: `LvDomain.Enums.PayrollStatus { Pending, Paid }`, `LvDomain.Enums.PayrollPaymentType { Full, Advance, Vacation, Overtime }`, `LvDomain.Enums.PaymentMethod { Transfer, Cash }` — every later task depends on these three.

- [ ] **Step 1: Create the three enum files**

`LvDomain/Enums/PayrollStatus.cs`:
```csharp
namespace LvDomain.Enums;

public enum PayrollStatus
{
    Pending,
    Paid
}
```

`LvDomain/Enums/PayrollPaymentType.cs`:
```csharp
namespace LvDomain.Enums;

public enum PayrollPaymentType
{
    Full,
    Advance,
    Vacation,
    Overtime
}
```

`LvDomain/Enums/PaymentMethod.cs`:
```csharp
namespace LvDomain.Enums;

public enum PaymentMethod
{
    Transfer,
    Cash
}
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build LvTest/LvDomain/LvDomain.csproj`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add LvTest/LvDomain/Enums/PayrollStatus.cs LvTest/LvDomain/Enums/PayrollPaymentType.cs LvTest/LvDomain/Enums/PaymentMethod.cs
git commit -m "feat(payroll): add Payroll enums"
```

---

### Task 2: Domain Entities

**Files:**
- Create: `LvDomain/Entities/Payroll/Payroll.cs`
- Create: `LvDomain/Entities/Payroll/PayrollDetail.cs`
- Create: `LvDomain/Entities/Payroll/PayrollDetailPayment.cs`

**Interfaces:**
- Consumes: `LvDomain.Enums.PayrollStatus`, `LvDomain.Enums.PayrollPaymentType`, `LvDomain.Enums.PaymentMethod` (Task 1); `LvDomain.Entities.Projects.Project`, `LvDomain.Entities.SiteLogs.SiteLog`, `LvDomain.Entities.Auth.User`, `LvDomain.Entities.Workers.Worker`, `LvDomain.Common.BaseEntity` (all pre-existing).
- Produces: `Payroll { Id, ProjectId, Project, SiteLogId, SiteLog, WeekStart, WeekEnd, TotalPayroll, Status, CreatedByUserId, CreatedByUser, PaidAt, Details }`; `PayrollDetail { Id, PayrollId, Payroll, WorkerId, Worker, Date, HoursWorked, HourlyRate, PaymentType, AdvanceAmountApplied, FinalAmountToPay, Payments }`; `PayrollDetailPayment { Id, PayrollDetailId, PayrollDetail, PaymentMethod, Amount }` — every later task depends on these exact property names.

- [ ] **Step 1: Create `Payroll.cs`**

```csharp
using LvDomain.Common;
using LvDomain.Entities.Auth;
using LvDomain.Entities.Projects;
using LvDomain.Entities.SiteLogs;
using LvDomain.Enums;

namespace LvDomain.Entities.Payroll;

public class Payroll : BaseEntity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int SiteLogId { get; set; }
    public SiteLog SiteLog { get; set; } = null!;

    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }

    public decimal TotalPayroll { get; set; }

    public PayrollStatus Status { get; set; }

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public DateTime? PaidAt { get; set; }

    public ICollection<PayrollDetail> Details { get; set; } = new List<PayrollDetail>();
}
```

- [ ] **Step 2: Create `PayrollDetail.cs`**

```csharp
using LvDomain.Common;
using LvDomain.Entities.Workers;
using LvDomain.Enums;

namespace LvDomain.Entities.Payroll;

public class PayrollDetail : BaseEntity
{
    public int PayrollId { get; set; }
    public Payroll Payroll { get; set; } = null!;

    public int WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    public DateTime Date { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal HourlyRate { get; set; }
    public PayrollPaymentType PaymentType { get; set; }
    public decimal? AdvanceAmountApplied { get; set; }
    public decimal FinalAmountToPay { get; set; }

    public ICollection<PayrollDetailPayment> Payments { get; set; } = new List<PayrollDetailPayment>();
}
```

- [ ] **Step 3: Create `PayrollDetailPayment.cs`**

```csharp
using LvDomain.Common;
using LvDomain.Enums;

namespace LvDomain.Entities.Payroll;

public class PayrollDetailPayment : BaseEntity
{
    public int PayrollDetailId { get; set; }
    public PayrollDetail PayrollDetail { get; set; } = null!;

    public PaymentMethod PaymentMethod { get; set; }
    public decimal Amount { get; set; }
}
```

- [ ] **Step 4: Build to verify it compiles**

Run: `dotnet build LvTest/LvDomain/LvDomain.csproj`
Expected: `Build succeeded. 0 Error(s)` (this also confirms the `LvDomain.Entities.Payroll` namespace and the `Payroll` class sharing a name is not ambiguous)

- [ ] **Step 5: Commit**

```bash
git add LvTest/LvDomain/Entities/Payroll/Payroll.cs LvTest/LvDomain/Entities/Payroll/PayrollDetail.cs LvTest/LvDomain/Entities/Payroll/PayrollDetailPayment.cs
git commit -m "feat(payroll): add Payroll domain entities"
```

---

### Task 3: DTOs

**Files:**
- Create: `LvApplication/DTOs/Payroll/PayrollDetailPaymentDto.cs`
- Create: `LvApplication/DTOs/Payroll/PayrollDetailDto.cs`
- Create: `LvApplication/DTOs/Payroll/CreatePayrollDto.cs`
- Create: `LvApplication/DTOs/Payroll/UpdatePayrollDto.cs`
- Create: `LvApplication/DTOs/Payroll/PayrollDetailPaymentResponseDto.cs`
- Create: `LvApplication/DTOs/Payroll/PayrollDetailResponseDto.cs`
- Create: `LvApplication/DTOs/Payroll/PayrollDto.cs`

**Interfaces:**
- Consumes: `LvDomain.Enums.PayrollPaymentType`, `LvDomain.Enums.PaymentMethod`, `LvDomain.Enums.PayrollStatus` (Task 1).
- Produces: `PayrollDetailPaymentDto { PaymentMethod, Amount }`; `PayrollDetailDto { WorkerId, Date, HoursWorked, HourlyRate, PaymentType, AdvanceAmountApplied, Payments }`; `CreatePayrollDto { SiteLogId, Details }`; `UpdatePayrollDto { Details }`; `PayrollDetailPaymentResponseDto { Id, PaymentMethod, Amount }`; `PayrollDetailResponseDto { Id, WorkerId, Date, HoursWorked, HourlyRate, PaymentType, AdvanceAmountApplied, FinalAmountToPay, Payments }`; `PayrollDto { Id, ProjectId, SiteLogId, WeekStart, WeekEnd, TotalPayroll, Status, CreatedByUserId, PaidAt, Details }` — Tasks 4, 5, 7, 8, 9 depend on these exact shapes.

- [ ] **Step 1: Create the input DTOs**

`LvApplication/DTOs/Payroll/PayrollDetailPaymentDto.cs`:
```csharp
using LvDomain.Enums;

namespace LvApplication.DTOs.Payroll;

public class PayrollDetailPaymentDto
{
    public PaymentMethod PaymentMethod { get; set; }
    public decimal Amount { get; set; }
}
```

`LvApplication/DTOs/Payroll/PayrollDetailDto.cs`:
```csharp
using LvDomain.Enums;

namespace LvApplication.DTOs.Payroll;

public class PayrollDetailDto
{
    public int WorkerId { get; set; }
    public DateTime Date { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal HourlyRate { get; set; }
    public PayrollPaymentType PaymentType { get; set; }
    public decimal? AdvanceAmountApplied { get; set; }
    public List<PayrollDetailPaymentDto> Payments { get; set; } = new();
}
```

`LvApplication/DTOs/Payroll/CreatePayrollDto.cs`:
```csharp
namespace LvApplication.DTOs.Payroll;

public class CreatePayrollDto
{
    public int SiteLogId { get; set; }
    public List<PayrollDetailDto> Details { get; set; } = new();
}
```

`LvApplication/DTOs/Payroll/UpdatePayrollDto.cs`:
```csharp
namespace LvApplication.DTOs.Payroll;

public class UpdatePayrollDto
{
    public List<PayrollDetailDto> Details { get; set; } = new();
}
```

- [ ] **Step 2: Create the response DTOs**

`LvApplication/DTOs/Payroll/PayrollDetailPaymentResponseDto.cs`:
```csharp
using LvDomain.Enums;

namespace LvApplication.DTOs.Payroll;

public class PayrollDetailPaymentResponseDto
{
    public int Id { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal Amount { get; set; }
}
```

`LvApplication/DTOs/Payroll/PayrollDetailResponseDto.cs`:
```csharp
using LvDomain.Enums;

namespace LvApplication.DTOs.Payroll;

public class PayrollDetailResponseDto
{
    public int Id { get; set; }
    public int WorkerId { get; set; }
    public DateTime Date { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal HourlyRate { get; set; }
    public PayrollPaymentType PaymentType { get; set; }
    public decimal? AdvanceAmountApplied { get; set; }
    public decimal FinalAmountToPay { get; set; }
    public List<PayrollDetailPaymentResponseDto> Payments { get; set; } = new();
}
```

`LvApplication/DTOs/Payroll/PayrollDto.cs`:
```csharp
using LvDomain.Enums;

namespace LvApplication.DTOs.Payroll;

public class PayrollDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int SiteLogId { get; set; }
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public decimal TotalPayroll { get; set; }
    public PayrollStatus Status { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime? PaidAt { get; set; }
    public List<PayrollDetailResponseDto> Details { get; set; } = new();
}
```

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build LvTest/LvApplication/LvApplication.csproj`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add LvTest/LvApplication/DTOs/Payroll/
git commit -m "feat(payroll): add Payroll DTOs"
```

---

### Task 4: EF Core Configuration + DbContext registration

**Files:**
- Create: `LvInfrastructure/Persistence/Configurations/Payroll/PayrollConfiguration.cs`
- Create: `LvInfrastructure/Persistence/Configurations/Payroll/PayrollDetailConfiguration.cs`
- Create: `LvInfrastructure/Persistence/Configurations/Payroll/PayrollDetailPaymentConfiguration.cs`
- Modify: `LvInfrastructure/Persistence/AppDbContext.cs`

**Interfaces:**
- Consumes: `Payroll`, `PayrollDetail`, `PayrollDetailPayment` (Task 2).
- Produces: `AppDbContext.Payrolls`, `AppDbContext.PayrollDetails`, `AppDbContext.PayrollDetailPayments` `DbSet`s — Task 5 (repository) and Task 8 (tests) depend on these.

- [ ] **Step 1: Create `PayrollConfiguration.cs`**

```csharp
using LvDomain.Entities.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Payroll;

public class PayrollConfiguration : IEntityTypeConfiguration<LvDomain.Entities.Payroll.Payroll>
{
    public void Configure(EntityTypeBuilder<LvDomain.Entities.Payroll.Payroll> builder)
    {
        builder.ToTable("Payrolls");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.TotalPayroll).HasColumnType("decimal(18,2)");

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasOne(p => p.Project)
            .WithMany()
            .HasForeignKey(p => p.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.SiteLog)
            .WithMany()
            .HasForeignKey(p => p.SiteLogId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.SiteLogId).IsUnique();

        builder.HasOne(p => p.CreatedByUser)
            .WithMany()
            .HasForeignKey(p => p.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

Note: the type parameter is fully qualified as `LvDomain.Entities.Payroll.Payroll` here only because this file's own namespace (`LvInfrastructure.Persistence.Configurations.Payroll`) also ends in `.Payroll`, so a bare `using LvDomain.Entities.Payroll;` plus a bare `Payroll` reference would resolve fine too — but being fully qualified on the `IEntityTypeConfiguration<...>` line removes any doubt for the reader. Do not add a `using LvDomain.Entities.Payroll;` line to this specific file since the type is already fully qualified twice.

- [ ] **Step 2: Create `PayrollDetailConfiguration.cs`**

```csharp
using LvDomain.Entities.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Payroll;

public class PayrollDetailConfiguration : IEntityTypeConfiguration<PayrollDetail>
{
    public void Configure(EntityTypeBuilder<PayrollDetail> builder)
    {
        builder.ToTable("PayrollDetails");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.HoursWorked).HasColumnType("decimal(5,2)");
        builder.Property(d => d.HourlyRate).HasColumnType("decimal(18,2)");
        builder.Property(d => d.AdvanceAmountApplied).HasColumnType("decimal(18,2)");
        builder.Property(d => d.FinalAmountToPay).HasColumnType("decimal(18,2)");

        builder.Property(d => d.PaymentType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasOne(d => d.Payroll)
            .WithMany(p => p.Details)
            .HasForeignKey(d => d.PayrollId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(d => d.PayrollId);

        builder.HasOne(d => d.Worker)
            .WithMany()
            .HasForeignKey(d => d.WorkerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(d => d.WorkerId);
    }
}
```

- [ ] **Step 3: Create `PayrollDetailPaymentConfiguration.cs`**

```csharp
using LvDomain.Entities.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Payroll;

public class PayrollDetailPaymentConfiguration : IEntityTypeConfiguration<PayrollDetailPayment>
{
    public void Configure(EntityTypeBuilder<PayrollDetailPayment> builder)
    {
        builder.ToTable("PayrollDetailPayments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount).HasColumnType("decimal(18,2)");

        builder.Property(p => p.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(p => p.PayrollDetail)
            .WithMany(d => d.Payments)
            .HasForeignKey(p => p.PayrollDetailId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(p => p.PayrollDetailId);
    }
}
```

- [ ] **Step 4: Register the three `DbSet`s in `AppDbContext.cs`**

In `LvInfrastructure/Persistence/AppDbContext.cs`, add to the `using` block:

```csharp
using LvDomain.Entities.Payroll;
```

Add after the `SiteLog` `DbSet`s (after line `public DbSet<SiteLogEquipment> SiteLogEquipment => Set<SiteLogEquipment>();`):

```csharp

    public DbSet<Payroll> Payrolls => Set<Payroll>();
    public DbSet<PayrollDetail> PayrollDetails => Set<PayrollDetail>();
    public DbSet<PayrollDetailPayment> PayrollDetailPayments => Set<PayrollDetailPayment>();
```

Do not touch `OnModelCreating` — `ApplyConfigurationsFromAssembly` already picks up the three new configuration classes automatically.

- [ ] **Step 5: Build to verify it compiles**

Run: `dotnet build LvTest/LvInfrastructure/LvInfrastructure.csproj`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 6: Commit**

```bash
git add LvTest/LvInfrastructure/Persistence/Configurations/Payroll/ LvTest/LvInfrastructure/Persistence/AppDbContext.cs
git commit -m "feat(payroll): add Payroll EF Core configuration"
```

---

### Task 5: Repository

**Files:**
- Create: `LvApplication/Services/Payroll/IPayrollRepository.cs`
- Create: `LvInfrastructure/Repositories/Payroll/PayrollRepository.cs`

**Interfaces:**
- Consumes: `Payroll` (Task 2), `AppDbContext.Payrolls` (Task 4).
- Produces: `IPayrollRepository { GetByIdAsync(int), ExistsForSiteLogAsync(int), GetPagedAsync(int,int), GetPagedByProjectAsync(int,int,int), AddAsync(Payroll), UpdateAsync(Payroll), DeleteAsync(Payroll) }` — Task 7 (service) and Task 8 (tests) depend on this exact signature set.

- [ ] **Step 1: Create `IPayrollRepository.cs`**

```csharp
using LvDomain.Entities.Payroll;

namespace LvApplication.Services.Payroll;

public interface IPayrollRepository
{
    Task<Payroll?> GetByIdAsync(int id);
    Task<bool> ExistsForSiteLogAsync(int siteLogId);
    Task<(List<Payroll> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);
    Task<(List<Payroll> Items, int TotalCount)> GetPagedByProjectAsync(int projectId, int pageNumber, int pageSize);
    Task AddAsync(Payroll payroll);
    Task UpdateAsync(Payroll payroll);
    Task DeleteAsync(Payroll payroll);
}
```

- [ ] **Step 2: Create `PayrollRepository.cs`**

```csharp
using LvApplication.Services.Payroll;
using LvDomain.Entities.Payroll;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LvInfrastructure.Repositories.Payroll;

public class PayrollRepository : IPayrollRepository
{
    private readonly AppDbContext _context;

    public PayrollRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<Payroll> PayrollsWithDetails => _context.Payrolls
        .Include(p => p.Details).ThenInclude(d => d.Payments);

    public Task<Payroll?> GetByIdAsync(int id) =>
        PayrollsWithDetails.FirstOrDefaultAsync(p => p.Id == id);

    public Task<bool> ExistsForSiteLogAsync(int siteLogId) =>
        _context.Payrolls.AnyAsync(p => p.SiteLogId == siteLogId);

    public async Task<(List<Payroll> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize)
    {
        var totalCount = await _context.Payrolls.CountAsync();

        var items = await PayrollsWithDetails
            .OrderBy(p => p.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(List<Payroll> Items, int TotalCount)> GetPagedByProjectAsync(int projectId, int pageNumber, int pageSize)
    {
        var query = _context.Payrolls.Where(p => p.ProjectId == projectId);

        var totalCount = await query.CountAsync();

        var items = await PayrollsWithDetails
            .Where(p => p.ProjectId == projectId)
            .OrderBy(p => p.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddAsync(Payroll payroll)
    {
        _context.Payrolls.Add(payroll);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Payroll payroll)
    {
        _context.Payrolls.Update(payroll);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Payroll payroll)
    {
        _context.Payrolls.Remove(payroll);
        await _context.SaveChangesAsync();
    }
}
```

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build LvTest/LvInfrastructure/LvInfrastructure.csproj`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add LvTest/LvApplication/Services/Payroll/IPayrollRepository.cs LvTest/LvInfrastructure/Repositories/Payroll/PayrollRepository.cs
git commit -m "feat(payroll): add Payroll repository"
```

---

### Task 6: Validators

**Files:**
- Create: `LvApplication/Validators/Payroll/PayrollDetailDtoValidator.cs`
- Create: `LvApplication/Validators/Payroll/CreatePayrollDtoValidator.cs`
- Create: `LvApplication/Validators/Payroll/UpdatePayrollDtoValidator.cs`

**Interfaces:**
- Consumes: `PayrollDetailDto`, `CreatePayrollDto`, `UpdatePayrollDto` (Task 3).
- Produces: `IValidator<CreatePayrollDto>`, `IValidator<UpdatePayrollDto>` (auto-registered by assembly scan) — Task 7 (service) and Task 8 (tests) depend on these being resolvable/constructible with no-arg constructors.

- [ ] **Step 1: Create the shared child validator `PayrollDetailDtoValidator.cs`**

This mirrors `LvApplication/Validators/Budgets/BudgetChapterDtoValidator.cs`'s reusable-child-validator pattern, and encodes the cross-field rule: `HoursWorked > 0`, `HourlyRate >= 0`, and the sum of `Payments[].Amount` must equal the computed `FinalAmountToPay` (`HoursWorked * HourlyRate - (AdvanceAmountApplied ?? 0)`).

```csharp
using FluentValidation;
using LvApplication.DTOs.Payroll;

namespace LvApplication.Validators.Payroll;

public class PayrollDetailDtoValidator : AbstractValidator<PayrollDetailDto>
{
    public PayrollDetailDtoValidator()
    {
        RuleFor(d => d.WorkerId).GreaterThan(0);
        RuleFor(d => d.HoursWorked).GreaterThan(0);
        RuleFor(d => d.HourlyRate).GreaterThanOrEqualTo(0);

        RuleFor(d => d)
            .Must(d =>
            {
                var finalAmountToPay = (d.HoursWorked * d.HourlyRate) - (d.AdvanceAmountApplied ?? 0);
                return d.Payments.Sum(p => p.Amount) == finalAmountToPay;
            })
            .WithMessage("La suma de los pagos de cada detalle debe coincidir con el monto final a pagar.");
    }
}
```

- [ ] **Step 2: Create `CreatePayrollDtoValidator.cs`**

```csharp
using FluentValidation;
using LvApplication.DTOs.Payroll;

namespace LvApplication.Validators.Payroll;

public class CreatePayrollDtoValidator : AbstractValidator<CreatePayrollDto>
{
    public CreatePayrollDtoValidator()
    {
        RuleFor(x => x.SiteLogId).GreaterThan(0);

        RuleForEach(x => x.Details).SetValidator(new PayrollDetailDtoValidator());
    }
}
```

- [ ] **Step 3: Create `UpdatePayrollDtoValidator.cs`**

```csharp
using FluentValidation;
using LvApplication.DTOs.Payroll;

namespace LvApplication.Validators.Payroll;

public class UpdatePayrollDtoValidator : AbstractValidator<UpdatePayrollDto>
{
    public UpdatePayrollDtoValidator()
    {
        RuleForEach(x => x.Details).SetValidator(new PayrollDetailDtoValidator());
    }
}
```

- [ ] **Step 4: Build to verify it compiles**

Run: `dotnet build LvTest/LvApplication/LvApplication.csproj`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 5: Commit**

```bash
git add LvTest/LvApplication/Validators/Payroll/
git commit -m "feat(payroll): add Payroll validators"
```

---

### Task 7: PayrollService — interface, CreateAsync, UpdateAsync

**Files:**
- Create: `LvApplication/Services/Payroll/IPayrollService.cs`
- Create: `LvApplication/Services/Payroll/PayrollService.cs`
- Modify: `LvTest/Common/ServiceFactory.cs`
- Test: `LvTest/Services/Payroll/PayrollServiceTests.cs`

**Interfaces:**
- Consumes: `IPayrollRepository` (Task 5), `IValidator<CreatePayrollDto>`/`IValidator<UpdatePayrollDto>` (Task 6), `ISiteLogService.GetByIdAsync(int)` returning `SiteLogDto { Id, ProjectId, WeekStart, WeekEnd, Status }` and `ISiteLogService.UpdateTotalPayrollAsync(int, decimal)` (pre-existing, `LvApplication/Services/SiteLogs/ISiteLogService.cs`), `IProjectRepository.GetByIdAsync(int)`/`UpdateAsync(Project)` (pre-existing), `IProjectService.IncrementWeekCounterAsync(int)` (pre-existing, `LvApplication/Services/Projects/IProjectService.cs`).
- Produces: `IPayrollService { CreateAsync(CreatePayrollDto, int), UpdateAsync(int, UpdatePayrollDto), MarkAsPaidAsync(int), DeleteAsync(int), GetByIdAsync(int), GetAllAsync(int,int), GetAllByProjectAsync(int,int,int) }` — Task 8 (rest of service) and Task 9 (controller) depend on this full signature set. This task implements the full class (all interface members must compile) but only `CreateAsync`/`UpdateAsync` get dedicated tests here; `MarkAsPaidAsync`/`DeleteAsync`/`Get*` are implemented in full now (they are short) and get their dedicated tests in Task 8.

- [ ] **Step 1: Create `IPayrollService.cs`**

```csharp
using LvApplication.Common;
using LvApplication.DTOs.Payroll;

namespace LvApplication.Services.Payroll;

public interface IPayrollService
{
    Task<PayrollDto> CreateAsync(CreatePayrollDto request, int createdByUserId);
    Task<PayrollDto> UpdateAsync(int id, UpdatePayrollDto request);
    Task<PayrollDto> MarkAsPaidAsync(int id);
    Task DeleteAsync(int id);
    Task<PayrollDto> GetByIdAsync(int id);
    Task<PagedResult<PayrollDto>> GetAllAsync(int pageNumber, int pageSize);
    Task<PagedResult<PayrollDto>> GetAllByProjectAsync(int projectId, int pageNumber, int pageSize);
}
```

- [ ] **Step 2: Create the full `PayrollService.cs`**

```csharp
using FluentValidation;
using LvApplication.Common;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Payroll;
using LvApplication.Services.Projects;
using LvApplication.Services.SiteLogs;
using LvDomain.Entities.Payroll;
using LvDomain.Enums;

namespace LvApplication.Services.Payroll;

public class PayrollService : IPayrollService
{
    private readonly IPayrollRepository _payrollRepository;
    private readonly ISiteLogService _siteLogService;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectService _projectService;
    private readonly IValidator<CreatePayrollDto> _createValidator;
    private readonly IValidator<UpdatePayrollDto> _updateValidator;

    public PayrollService(
        IPayrollRepository payrollRepository,
        ISiteLogService siteLogService,
        IProjectRepository projectRepository,
        IProjectService projectService,
        IValidator<CreatePayrollDto> createValidator,
        IValidator<UpdatePayrollDto> updateValidator)
    {
        _payrollRepository = payrollRepository;
        _siteLogService = siteLogService;
        _projectRepository = projectRepository;
        _projectService = projectService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PayrollDto> CreateAsync(CreatePayrollDto request, int createdByUserId)
    {
        await _createValidator.ValidateAndThrowAppExceptionAsync(request);

        var siteLog = await _siteLogService.GetByIdAsync(request.SiteLogId);

        if (siteLog.Status != SiteLogStatus.Approved)
        {
            throw new ValidationAppException("Solo se puede crear una planilla a partir de una bitácora Aprobada.");
        }

        if (await _payrollRepository.ExistsForSiteLogAsync(request.SiteLogId))
        {
            throw new ConflictException("Ya existe una planilla para esta bitácora.");
        }

        var payroll = new LvDomain.Entities.Payroll.Payroll
        {
            ProjectId = siteLog.ProjectId,
            SiteLogId = siteLog.Id,
            WeekStart = siteLog.WeekStart,
            WeekEnd = siteLog.WeekEnd,
            Status = PayrollStatus.Pending,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        SyncDetails(payroll, request.Details);
        payroll.TotalPayroll = payroll.Details.Sum(d => d.FinalAmountToPay);

        await _payrollRepository.AddAsync(payroll);

        return MapToDto(payroll);
    }

    public async Task<PayrollDto> UpdateAsync(int id, UpdatePayrollDto request)
    {
        await _updateValidator.ValidateAndThrowAppExceptionAsync(request);

        var payroll = await _payrollRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Payroll {id} not found.");

        if (payroll.Status != PayrollStatus.Pending)
        {
            throw new ValidationAppException("Solo se puede editar una planilla en estado Pendiente.");
        }

        payroll.UpdatedAt = DateTime.UtcNow;

        SyncDetails(payroll, request.Details);
        payroll.TotalPayroll = payroll.Details.Sum(d => d.FinalAmountToPay);

        await _payrollRepository.UpdateAsync(payroll);

        return MapToDto(payroll);
    }

    public async Task<PayrollDto> MarkAsPaidAsync(int id)
    {
        var payroll = await _payrollRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Payroll {id} not found.");

        if (payroll.Status != PayrollStatus.Pending)
        {
            throw new ValidationAppException("Solo se puede marcar como pagada una planilla en estado Pendiente.");
        }

        payroll.Status = PayrollStatus.Paid;
        payroll.PaidAt = DateTime.UtcNow;
        payroll.UpdatedAt = DateTime.UtcNow;
        await _payrollRepository.UpdateAsync(payroll);

        var project = await _projectRepository.GetByIdAsync(payroll.ProjectId)
            ?? throw new NotFoundException($"Project {payroll.ProjectId} not found.");
        project.CurrentDirectExpenses += payroll.TotalPayroll;
        project.UpdatedAt = DateTime.UtcNow;
        await _projectRepository.UpdateAsync(project);

        await _siteLogService.UpdateTotalPayrollAsync(payroll.SiteLogId, payroll.TotalPayroll);
        await _projectService.IncrementWeekCounterAsync(payroll.ProjectId);

        return MapToDto(payroll);
    }

    public async Task DeleteAsync(int id)
    {
        var payroll = await _payrollRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Payroll {id} not found.");

        if (payroll.Status != PayrollStatus.Pending)
        {
            throw new ValidationAppException("Solo se puede eliminar una planilla en estado Pendiente.");
        }

        await _payrollRepository.DeleteAsync(payroll);
    }

    public async Task<PayrollDto> GetByIdAsync(int id)
    {
        var payroll = await _payrollRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Payroll {id} not found.");
        return MapToDto(payroll);
    }

    public async Task<PagedResult<PayrollDto>> GetAllAsync(int pageNumber, int pageSize)
    {
        var (items, totalCount) = await _payrollRepository.GetPagedAsync(pageNumber, pageSize);

        return new PagedResult<PayrollDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<PayrollDto>> GetAllByProjectAsync(int projectId, int pageNumber, int pageSize)
    {
        var (items, totalCount) = await _payrollRepository.GetPagedByProjectAsync(projectId, pageNumber, pageSize);

        return new PagedResult<PayrollDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    private static void SyncDetails(LvDomain.Entities.Payroll.Payroll payroll, List<PayrollDetailDto> detailDtos)
    {
        payroll.Details.Clear();

        foreach (var dto in detailDtos)
        {
            var finalAmountToPay = (dto.HoursWorked * dto.HourlyRate) - (dto.AdvanceAmountApplied ?? 0);

            var detail = new PayrollDetail
            {
                WorkerId = dto.WorkerId,
                Date = dto.Date,
                HoursWorked = dto.HoursWorked,
                HourlyRate = dto.HourlyRate,
                PaymentType = dto.PaymentType,
                AdvanceAmountApplied = dto.AdvanceAmountApplied,
                FinalAmountToPay = finalAmountToPay,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var paymentDto in dto.Payments)
            {
                detail.Payments.Add(new PayrollDetailPayment
                {
                    PaymentMethod = paymentDto.PaymentMethod,
                    Amount = paymentDto.Amount,
                    CreatedAt = DateTime.UtcNow
                });
            }

            payroll.Details.Add(detail);
        }
    }

    private static PayrollDto MapToDto(LvDomain.Entities.Payroll.Payroll payroll) => new()
    {
        Id = payroll.Id,
        ProjectId = payroll.ProjectId,
        SiteLogId = payroll.SiteLogId,
        WeekStart = payroll.WeekStart,
        WeekEnd = payroll.WeekEnd,
        TotalPayroll = payroll.TotalPayroll,
        Status = payroll.Status,
        CreatedByUserId = payroll.CreatedByUserId,
        PaidAt = payroll.PaidAt,
        Details = payroll.Details.Select(d => new PayrollDetailResponseDto
        {
            Id = d.Id,
            WorkerId = d.WorkerId,
            Date = d.Date,
            HoursWorked = d.HoursWorked,
            HourlyRate = d.HourlyRate,
            PaymentType = d.PaymentType,
            AdvanceAmountApplied = d.AdvanceAmountApplied,
            FinalAmountToPay = d.FinalAmountToPay,
            Payments = d.Payments.Select(p => new PayrollDetailPaymentResponseDto
            {
                Id = p.Id,
                PaymentMethod = p.PaymentMethod,
                Amount = p.Amount
            }).ToList()
        }).ToList()
    };
}
```

Note: `LvDomain.Entities.Payroll.Payroll` is fully qualified everywhere it's used as a type in this file (constructor, parameter types) because this file's own namespace is `LvApplication.Services.Payroll` (also ending in `.Payroll`) — being explicit here avoids any reader confusion, even though a bare `using LvDomain.Entities.Payroll;` would also compile correctly.

- [ ] **Step 3: Add `CreatePayrollService` to `LvTest/Common/ServiceFactory.cs`**

Add these `using` statements to the top of the file (alongside the existing ones):

```csharp
using LvApplication.Services.Payroll;
using LvApplication.Validators.Payroll;
using LvInfrastructure.Repositories.Payroll;
```

Add this method at the end of the `ServiceFactory` class (after `CreateSiteLogService`):

```csharp

    public static PayrollService CreatePayrollService(AppDbContext context) =>
        new(
            new PayrollRepository(context),
            CreateSiteLogService(context),
            new ProjectRepository(context),
            CreateProjectService(context),
            new CreatePayrollDtoValidator(),
            new UpdatePayrollDtoValidator());
```

- [ ] **Step 4: Write the failing tests for `CreateAsync` and `UpdateAsync`**

Create `LvTest/Services/Payroll/PayrollServiceTests.cs`:

```csharp
using FluentAssertions;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Payroll;
using LvApplication.DTOs.Projects;
using LvApplication.DTOs.SiteLogs;
using LvDomain.Entities.Branches;
using LvDomain.Entities.Budgets;
using LvDomain.Entities.Customers;
using LvDomain.Entities.Offers;
using LvDomain.Entities.Workers;
using LvDomain.Enums;
using LvInfrastructure.Persistence;
using LvTest.Common;
using Microsoft.EntityFrameworkCore;

namespace LvTest.Services.Payroll;

public class PayrollServiceTests
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

    private static async Task<Worker> CreateWorkerAsync(AppDbContext context, string name = "Trabajador Test")
    {
        var worker = new Worker
        {
            Name = name,
            Status = ActiveStatus.Active,
            Category = WorkerCategory.Construction,
            Type = WorkerType.Laborer,
            HourlyRate = 5m,
            CreatedAt = DateTime.UtcNow
        };
        context.Workers.Add(worker);
        await context.SaveChangesAsync();
        return worker;
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

    private static async Task<SiteLogDto> CreateApprovedSiteLogAsync(
        AppDbContext context,
        int projectId,
        int projectAdminId,
        int managerId,
        DateTime weekStart)
    {
        var siteLogService = ServiceFactory.CreateSiteLogService(context);

        var created = await siteLogService.CreateAsync(new CreateSiteLogDto
        {
            ProjectId = projectId,
            WeekStart = weekStart,
            WeekEnd = weekStart.AddDays(6),
            TaskDescription = "Semana de trabajo",
            Workers = new List<SiteLogWorkerDto>(),
            Materials = new List<SiteLogMaterialDto>(),
            Equipment = new List<SiteLogEquipmentDto>()
        }, projectAdminId);

        await siteLogService.SubmitToReviewAsync(created.Id);
        return await siteLogService.ApproveAsync(created.Id, managerId);
    }

    private static PayrollDetailDto BuildDetail(
        int workerId,
        DateTime date,
        decimal hoursWorked,
        decimal hourlyRate,
        PayrollPaymentType paymentType = PayrollPaymentType.Full,
        decimal? advanceAmountApplied = null,
        List<PayrollDetailPaymentDto>? payments = null)
    {
        var finalAmountToPay = (hoursWorked * hourlyRate) - (advanceAmountApplied ?? 0);

        return new PayrollDetailDto
        {
            WorkerId = workerId,
            Date = date,
            HoursWorked = hoursWorked,
            HourlyRate = hourlyRate,
            PaymentType = paymentType,
            AdvanceAmountApplied = advanceAmountApplied,
            Payments = payments ?? new List<PayrollDetailPaymentDto>
            {
                new() { PaymentMethod = PaymentMethod.Transfer, Amount = finalAmountToPay }
            }
        };
    }

    [Fact]
    public async Task CreateAsync_SiteLogNotFound_ThrowsNotFoundException()
    {
        using var context = TestDbContextFactory.Create();
        var service = ServiceFactory.CreatePayrollService(context);

        var act = async () => await service.CreateAsync(new CreatePayrollDto { SiteLogId = 9999, Details = new() }, createdByUserId: 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_SiteLogNotApproved_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var (project, _, projectAdminId) = await CreateActiveProjectAsync(context);
        var siteLogService = ServiceFactory.CreateSiteLogService(context);

        var siteLog = await siteLogService.CreateAsync(new CreateSiteLogDto
        {
            ProjectId = project.Id,
            WeekStart = new DateTime(2026, 3, 2),
            WeekEnd = new DateTime(2026, 3, 8),
            TaskDescription = "Semana de trabajo"
        }, projectAdminId);

        var service = ServiceFactory.CreatePayrollService(context);

        var act = async () => await service.CreateAsync(new CreatePayrollDto { SiteLogId = siteLog.Id, Details = new() }, projectAdminId);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task CreateAsync_DuplicateSiteLogId_ThrowsConflictException()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var siteLog = await CreateApprovedSiteLogAsync(context, project.Id, projectAdminId, managerId, new DateTime(2026, 3, 2));
        var service = ServiceFactory.CreatePayrollService(context);

        await service.CreateAsync(new CreatePayrollDto { SiteLogId = siteLog.Id, Details = new() }, projectAdminId);

        var act = async () => await service.CreateAsync(new CreatePayrollDto { SiteLogId = siteLog.Id, Details = new() }, projectAdminId);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CreateAsync_CalculatesFinalAmountAndTotalPayroll_WithAdvance()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var siteLog = await CreateApprovedSiteLogAsync(context, project.Id, projectAdminId, managerId, new DateTime(2026, 3, 2));
        var worker1 = await CreateWorkerAsync(context, "Trabajador 1");
        var worker2 = await CreateWorkerAsync(context, "Trabajador 2");
        var service = ServiceFactory.CreatePayrollService(context);

        var result = await service.CreateAsync(new CreatePayrollDto
        {
            SiteLogId = siteLog.Id,
            Details = new List<PayrollDetailDto>
            {
                BuildDetail(worker1.Id, new DateTime(2026, 3, 2), hoursWorked: 40, hourlyRate: 5), // 200
                BuildDetail(worker2.Id, new DateTime(2026, 3, 2), hoursWorked: 40, hourlyRate: 5, paymentType: PayrollPaymentType.Advance, advanceAmountApplied: 50) // 200 - 50 = 150
            }
        }, projectAdminId);

        result.Status.Should().Be(PayrollStatus.Pending);
        result.Details.Should().ContainSingle(d => d.WorkerId == worker1.Id && d.FinalAmountToPay == 200m);
        result.Details.Should().ContainSingle(d => d.WorkerId == worker2.Id && d.FinalAmountToPay == 150m);
        result.TotalPayroll.Should().Be(350m);
    }

    [Fact]
    public async Task CreateAsync_PaymentsSumMismatch_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var siteLog = await CreateApprovedSiteLogAsync(context, project.Id, projectAdminId, managerId, new DateTime(2026, 3, 2));
        var worker = await CreateWorkerAsync(context);
        var service = ServiceFactory.CreatePayrollService(context);

        var detail = BuildDetail(worker.Id, new DateTime(2026, 3, 2), hoursWorked: 40, hourlyRate: 5); // final = 200
        detail.Payments = new List<PayrollDetailPaymentDto>
        {
            new() { PaymentMethod = PaymentMethod.Transfer, Amount = 40 } // does not add up to 200
        };

        var act = async () => await service.CreateAsync(new CreatePayrollDto { SiteLogId = siteLog.Id, Details = new List<PayrollDetailDto> { detail } }, projectAdminId);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task UpdateAsync_InPending_SyncsAggregateCompletely()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var siteLog = await CreateApprovedSiteLogAsync(context, project.Id, projectAdminId, managerId, new DateTime(2026, 3, 2));
        var worker1 = await CreateWorkerAsync(context, "Trabajador 1");
        var worker2 = await CreateWorkerAsync(context, "Trabajador 2");
        var service = ServiceFactory.CreatePayrollService(context);

        var created = await service.CreateAsync(new CreatePayrollDto
        {
            SiteLogId = siteLog.Id,
            Details = new List<PayrollDetailDto> { BuildDetail(worker1.Id, new DateTime(2026, 3, 2), hoursWorked: 10, hourlyRate: 5) } // 50
        }, projectAdminId);

        var updated = await service.UpdateAsync(created.Id, new UpdatePayrollDto
        {
            Details = new List<PayrollDetailDto>
            {
                BuildDetail(worker2.Id, new DateTime(2026, 3, 3), hoursWorked: 20, hourlyRate: 5) // 100
            }
        });

        updated.Details.Should().ContainSingle();
        updated.Details.Should().ContainSingle(d => d.WorkerId == worker2.Id && d.FinalAmountToPay == 100m);
        updated.TotalPayroll.Should().Be(100m);
    }

    [Fact]
    public async Task UpdateAsync_Paid_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var siteLog = await CreateApprovedSiteLogAsync(context, project.Id, projectAdminId, managerId, new DateTime(2026, 3, 2));
        var service = ServiceFactory.CreatePayrollService(context);

        var created = await service.CreateAsync(new CreatePayrollDto { SiteLogId = siteLog.Id, Details = new() }, projectAdminId);
        await service.MarkAsPaidAsync(created.Id);

        var act = async () => await service.UpdateAsync(created.Id, new UpdatePayrollDto { Details = new() });

        await act.Should().ThrowAsync<ValidationAppException>();
    }
}
```

- [ ] **Step 5: Run the tests to verify they fail for the right reason first (before Step 2's implementation existed), then pass now**

Run: `dotnet test LvTest/LvTest/LvTest.csproj --filter "FullyQualifiedName~PayrollServiceTests"`
Expected: all 6 tests in this file PASS (`CreateAsync_SiteLogNotFound_ThrowsNotFoundException`, `CreateAsync_SiteLogNotApproved_ThrowsValidationException`, `CreateAsync_DuplicateSiteLogId_ThrowsConflictException`, `CreateAsync_CalculatesFinalAmountAndTotalPayroll_WithAdvance`, `CreateAsync_PaymentsSumMismatch_ThrowsValidationException`, `UpdateAsync_InPending_SyncsAggregateCompletely`, `UpdateAsync_Paid_ThrowsValidationException` — 7 tests total)

If you are following strict TDD (writing Step 4's test file before Step 2's implementation existed), running the tests at that point should fail with compile errors (`PayrollService`/`IPayrollService` not found) — that is the expected "red" state. Once Steps 1–3 exist, this run is the "green" state.

- [ ] **Step 6: Commit**

```bash
git add LvTest/LvApplication/Services/Payroll/IPayrollService.cs LvTest/LvApplication/Services/Payroll/PayrollService.cs LvTest/LvTest/Common/ServiceFactory.cs LvTest/LvTest/Services/Payroll/PayrollServiceTests.cs
git commit -m "feat(payroll): add PayrollService with Create/Update business rules"
```

---

### Task 8: PayrollService — MarkAsPaidAsync and DeleteAsync tests

**Files:**
- Test: `LvTest/Services/Payroll/PayrollServiceTests.cs` (append to the file created in Task 7)

**Interfaces:**
- Consumes: `PayrollService.MarkAsPaidAsync(int)`, `PayrollService.DeleteAsync(int)` (Task 7, already fully implemented) — this task only adds the tests that were deferred from Task 7.

- [ ] **Step 1: Write the failing tests**

Append these methods inside the `PayrollServiceTests` class (before the final closing `}`):

```csharp

    [Fact]
    public async Task MarkAsPaidAsync_AppliesAllSideEffects()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var siteLog = await CreateApprovedSiteLogAsync(context, project.Id, projectAdminId, managerId, new DateTime(2026, 3, 2));
        var worker = await CreateWorkerAsync(context);
        var service = ServiceFactory.CreatePayrollService(context);

        var storedProjectBefore = await context.Projects.FindAsync(project.Id);
        var directExpensesBefore = storedProjectBefore!.CurrentDirectExpenses;
        var weeksCounterBefore = storedProjectBefore.WeeksCounter;

        var created = await service.CreateAsync(new CreatePayrollDto
        {
            SiteLogId = siteLog.Id,
            Details = new List<PayrollDetailDto> { BuildDetail(worker.Id, new DateTime(2026, 3, 2), hoursWorked: 40, hourlyRate: 5) } // 200
        }, projectAdminId);

        var paid = await service.MarkAsPaidAsync(created.Id);

        paid.Status.Should().Be(PayrollStatus.Paid);
        paid.PaidAt.Should().NotBeNull();

        var storedProjectAfter = await context.Projects.FindAsync(project.Id);
        storedProjectAfter!.CurrentDirectExpenses.Should().Be(directExpensesBefore + 200m);
        storedProjectAfter.WeeksCounter.Should().Be(weeksCounterBefore + 1);

        var storedSiteLog = await context.SiteLogs.FindAsync(siteLog.Id);
        storedSiteLog!.TotalPayroll.Should().Be(200m);
    }

    [Fact]
    public async Task MarkAsPaidAsync_NotPending_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var siteLog = await CreateApprovedSiteLogAsync(context, project.Id, projectAdminId, managerId, new DateTime(2026, 3, 2));
        var service = ServiceFactory.CreatePayrollService(context);

        var created = await service.CreateAsync(new CreatePayrollDto { SiteLogId = siteLog.Id, Details = new() }, projectAdminId);
        await service.MarkAsPaidAsync(created.Id);

        var act = async () => await service.MarkAsPaidAsync(created.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task DeleteAsync_Pending_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var siteLog = await CreateApprovedSiteLogAsync(context, project.Id, projectAdminId, managerId, new DateTime(2026, 3, 2));
        var service = ServiceFactory.CreatePayrollService(context);

        var created = await service.CreateAsync(new CreatePayrollDto { SiteLogId = siteLog.Id, Details = new() }, projectAdminId);

        await service.DeleteAsync(created.Id);

        var stored = await context.Payrolls.FindAsync(created.Id);
        stored.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_Paid_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var (project, managerId, projectAdminId) = await CreateActiveProjectAsync(context);
        var siteLog = await CreateApprovedSiteLogAsync(context, project.Id, projectAdminId, managerId, new DateTime(2026, 3, 2));
        var service = ServiceFactory.CreatePayrollService(context);

        var created = await service.CreateAsync(new CreatePayrollDto { SiteLogId = siteLog.Id, Details = new() }, projectAdminId);
        await service.MarkAsPaidAsync(created.Id);

        var act = async () => await service.DeleteAsync(created.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }
```

- [ ] **Step 2: Run the tests**

Run: `dotnet test LvTest/LvTest/LvTest.csproj --filter "FullyQualifiedName~PayrollServiceTests"`
Expected: all 11 tests in `PayrollServiceTests` PASS.

- [ ] **Step 3: Commit**

```bash
git add LvTest/LvTest/Services/Payroll/PayrollServiceTests.cs
git commit -m "test(payroll): cover MarkAsPaid and Delete business rules"
```

---

### Task 9: Controller + DI registration

**Files:**
- Create: `LvApi/Controllers/Payroll/PayrollsController.cs`
- Modify: `LvApi/Extensions/ServiceCollectionExtensions.cs`

**Interfaces:**
- Consumes: `IPayrollService` (Task 7).
- Produces: REST endpoints `POST /api/payrolls`, `PUT /api/payrolls/{id}`, `POST /api/payrolls/{id}/mark-paid`, `DELETE /api/payrolls/{id}`, `GET /api/payrolls`, `GET /api/payrolls/{id}`, `GET /api/projects/{projectId}/payrolls`.

- [ ] **Step 1: Create `PayrollsController.cs`**

```csharp
using System.Security.Claims;
using LvApplication.Common;
using LvApplication.DTOs.Payroll;
using LvApplication.Services.Payroll;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.Payroll;

[ApiController]
[Route("api")]
[Authorize]
public class PayrollsController : ControllerBase
{
    private readonly IPayrollService _payrollService;

    public PayrollsController(IPayrollService payrollService)
    {
        _payrollService = payrollService;
    }

    [Authorize(Roles = "ProjectAdmin")]
    [HttpPost("payrolls")]
    public async Task<ActionResult<PayrollDto>> Create(CreatePayrollDto request)
    {
        var result = await _payrollService.CreateAsync(request, GetCurrentUserId());
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [Authorize(Roles = "ProjectAdmin")]
    [HttpPut("payrolls/{id:int}")]
    public async Task<ActionResult<PayrollDto>> Update(int id, UpdatePayrollDto request)
    {
        var result = await _payrollService.UpdateAsync(id, request);
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpPost("payrolls/{id:int}/mark-paid")]
    public async Task<ActionResult<PayrollDto>> MarkAsPaid(int id)
    {
        var result = await _payrollService.MarkAsPaidAsync(id);
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpDelete("payrolls/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _payrollService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("payrolls")]
    public async Task<ActionResult<PagedResult<PayrollDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _payrollService.GetAllAsync(pageNumber, pageSize);
        return Ok(result);
    }

    [HttpGet("payrolls/{id:int}")]
    public async Task<ActionResult<PayrollDto>> GetById(int id)
    {
        var result = await _payrollService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpGet("projects/{projectId:int}/payrolls")]
    public async Task<ActionResult<PagedResult<PayrollDto>>> GetAllByProject(
        int projectId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _payrollService.GetAllByProjectAsync(projectId, pageNumber, pageSize);
        return Ok(result);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        return int.Parse(userIdClaim!.Value);
    }
}
```

- [ ] **Step 2: Register the repository and service in `ServiceCollectionExtensions.cs`**

Add to the `using` block:

```csharp
using LvApplication.Services.Payroll;
using LvInfrastructure.Repositories.Payroll;
```

In `AddInfrastructureServices`, add after `services.AddScoped<ISiteLogRepository, SiteLogRepository>();`:

```csharp
        services.AddScoped<IPayrollRepository, PayrollRepository>();
```

In `AddApplicationServices`, add after `services.AddScoped<ISiteLogService, SiteLogService>();`:

```csharp
        services.AddScoped<IPayrollService, PayrollService>();
```

- [ ] **Step 3: Full solution build**

Run: `dotnet build LvTest/LvTest.sln` (or `dotnet build` from the `LvTest` directory if there is no `.sln`, building each project — use whichever the repo already uses)
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Full test suite run**

Run: `dotnet test LvTest/LvTest/LvTest.csproj`
Expected: all tests pass, including the 11 new `PayrollServiceTests`.

- [ ] **Step 5: Commit**

```bash
git add LvTest/LvApi/Controllers/Payroll/PayrollsController.cs LvTest/LvApi/Extensions/ServiceCollectionExtensions.cs
git commit -m "feat(payroll): add PayrollsController and wire up DI"
```

---

## Self-Review

**1. Spec coverage:**
- Entities `Payroll`/`PayrollDetail`/`PayrollDetailPayment` with every field from the spec → Task 2. ✅
- Enums `PayrollStatus`, `PayrollPaymentType`, `PaymentMethod` → Task 1. ✅
- `CreateAsync` (SiteLog exists+Approved, uniqueness, copy Project/Week fields, aggregate sync, `FinalAmountToPay`/`TotalPayroll` calc) → Task 7. ✅
- `UpdateAsync` (Pending-only, full aggregate replace) → Task 7. ✅
- `MarkAsPaidAsync` (GeneralManager/OperationsDirector via controller attribute, Pending-only, `CurrentDirectExpenses` += TotalPayroll, `UpdateTotalPayrollAsync` hook, `IncrementWeekCounterAsync` hook called once) → Task 7 (impl) + Task 8 (tests). ✅
- `DeleteAsync` (GeneralManager/OperationsDirector via controller attribute, Pending-only) → Task 7 (impl) + Task 8 (tests). ✅
- Advance rule (treated as an ordinary `PayrollDetail`, no separate entity) → Task 2/7 — `PayrollDetail.PaymentType == Advance` is just one of four enum values, no special-cased entity. ✅
- DTOs (`CreatePayrollDto`, `UpdatePayrollDto`, `PayrollDto`) → Task 3. ✅
- Validators (`SiteLogId > 0`, `HoursWorked > 0`, `HourlyRate >= 0`, Payments sum == FinalAmountToPay) → Task 6. ✅
- Repository + Service + Controller, all 7 endpoints → Tasks 5, 7, 9. ✅
- EF Core configuration incl. unique index on `Payrolls.SiteLogId` → Task 4. ✅
- No migration generated → explicitly called out in Global Constraints and never appears as a step. ✅
- Tests: all 8 bullet points from the spec's Task 8 section are covered 1:1 by named tests across Tasks 7–8 (`CreateAsync_SiteLogNotFound_ThrowsNotFoundException`/`CreateAsync_SiteLogNotApproved_ThrowsValidationException` together cover "rejected if SiteLog doesn't exist or isn't Approved"; `CreateAsync_DuplicateSiteLogId_ThrowsConflictException`; `CreateAsync_CalculatesFinalAmountAndTotalPayroll_WithAdvance`; `CreateAsync_PaymentsSumMismatch_ThrowsValidationException`; `UpdateAsync_InPending_SyncsAggregateCompletely`/`UpdateAsync_Paid_ThrowsValidationException`; `MarkAsPaidAsync_AppliesAllSideEffects`/`MarkAsPaidAsync_NotPending_ThrowsValidationException`; `DeleteAsync_Pending_Succeeds`/`DeleteAsync_Paid_ThrowsValidationException`). ✅

**2. Placeholder scan:** No `TBD`/`TODO`/"add validation"/"similar to Task N" phrases anywhere in the task steps — every step has complete, runnable code. ✅

**3. Type consistency:** `IPayrollRepository`, `IPayrollService`, DTO property names, and enum names are identical every place they're referenced across Tasks 3, 5, 6, 7, 8, 9 (cross-checked `PayrollDetailDto.Payments`, `PayrollDetailResponseDto.Payments`, `Payroll.Details`, `PayrollDetail.Payments`, `IPayrollRepository.ExistsForSiteLogAsync`/`GetPagedByProjectAsync` signatures used identically in `PayrollRepository` and `PayrollService`). ✅

One deliberate deviation from the literal spec: `docs/Especificacion_Sistema.md` does not exist anywhere in this repository (confirmed via search) — this plan was written directly from the exhaustive business-rule description already provided in the request, which is self-consistent and complete enough to implement without that file. Flag this to the user if the file was expected to exist elsewhere.

**Execution note (discovered during implementation, Task 5):** the plan's `IPayrollRepository.cs` and `PayrollRepository.cs` snippets use a bare `Payroll` type reference brought in via `using LvDomain.Entities.Payroll;`. This does **not** compile as written: both files live in a namespace whose last segment is also `Payroll` (`LvApplication.Services.Payroll` and `LvInfrastructure.Repositories.Payroll` respectively), and C# resolves the containing namespace's own nested-namespace name (`...Payroll`) before it considers `using`-imported types — so the compiler reports `error CS0118: 'Payroll' is a namespace but is used like a type`. `PayrollService.cs` was written from the start with `LvDomain.Entities.Payroll.Payroll` fully qualified (its in-file note called this out), but `IPayrollRepository.cs`/`PayrollRepository.cs` were not, and were fixed the same way during execution: drop the `using LvDomain.Entities.Payroll;` line and fully qualify every bare `Payroll` type reference as `LvDomain.Entities.Payroll.Payroll`. `PayrollDetail`/`PayrollDetailPayment` references are unaffected (different identifier, no collision) — only the exact identifier `Payroll` triggers this.
