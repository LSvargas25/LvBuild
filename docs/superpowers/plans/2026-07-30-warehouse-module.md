# Warehouse Module (Fase 13) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the Warehouse module (`InventoryMovement`, Bodega ↔ Comercio ↔ Proyecto transfers) reusing Product/BranchInventory/ProductIncorporationTicket from Fase 12, close two Fase-12 branch-type validation gaps (CashRegister/Invoice, ProductIncorporationTicket), and build the previously-nonexistent Worker→Branch assignment with Category/BranchType cross-validation.

**Architecture:** Follows the exact layered pattern already used for the Commercial module (Fase 12): `LvDomain/Entities/Warehouse` → `LvDomain/Enums` → `LvInfrastructure/Persistence/Configurations/Warehouse` → `LvApplication/DTOs/Warehouse` → `LvApplication/Validators/Warehouse` → `LvApplication/Services/Warehouse` (interface + impl + repository interface) → `LvInfrastructure/Repositories/Warehouse` → `LvApi/Controllers/Warehouse` → `LvTest/Services/Warehouse`. `InventoryMovementService.ValidateAsync` reuses the exact increment patterns already in the codebase: `ProductIncorporationTicketService.IncrementInventoryAsync`'s style for `BranchInventory` (destination is a Branch) and `MaterialTicketService.ApplyAsync`'s overwrite-cost style for `ProjectInventoryItem` (destination is a Project).

**Tech Stack:** .NET 8, EF Core 9 (SQL Server provider in production, InMemory provider in tests), FluentValidation, xUnit + FluentAssertions.

## Global Constraints

- Solution root: `C:\Users\steve\OneDrive\Desktop\Steven\Projects\Construccion\Backend\LvTest` (has `LvApi/LvApplication/LvDomain/LvInfrastructure/LvTest`).
- **NO EF Core migration in this phase** — schema changes (new `InventoryMovement` table, extended `ProjectInventoryItem`, extended `Workers`) are code-only. Do not run `dotnet ef migrations add`.
- **Do not `git commit`** unless the user explicitly asks — this session has not requested commits. Steps below say "mark step done", not "commit".
- Tests use `Microsoft.EntityFrameworkCore.InMemory` (`LvTest/Common/TestDbContextFactory.cs`) — it does **not** enforce `HasCheckConstraint` or `HasFilter`. These are still added for SQL Server correctness (defense in depth), but the tests that actually prove business rules are the **service-layer validation** tests, not schema tests.
- Role strings are raw comma-joined literals in `[Authorize(Roles="...")]` and private `const string` fields in services — there is no `RoleNames` constants class anywhere in the codebase. Follow this convention, don't introduce one.
- `TestUserFactory` (`LvTest/Common/TestUserFactory.cs`) role-id constants: `GeneralManagerRoleId=1, OperationsDirectorRoleId=2, ProjectAdminRoleId=3, BranchAdminRoleId=4, BusinessManagerRoleId=5`. Role name strings used in `actingUserRoles` arrays: `"GeneralManager"`, `"OperationsDirector"`, `"BranchAdmin"`, `"BusinessManager"`.
- `BranchType` enum (`LvDomain/Enums/BranchType.cs`) has exactly `Office, Commercial, Warehouse`. `Branch.BranchType` is the property name (not `.Type`).
- Exceptions (`LvApplication/Common/Exceptions/`, all single-string-ctor): `NotFoundException`, `ValidationAppException`, `ForbiddenException`, `ConflictException`.
- `IValidator<T>.ValidateAndThrowAppExceptionAsync(instance)` (`LvApplication/Common/ValidatorExtensions.cs`) is the standard way services invoke FluentValidation and convert failures to `ValidationAppException`.
- `AddApplicationServices()` calls `services.AddValidatorsFromAssembly(applicationAssembly)` — FluentValidation validators are auto-discovered and their constructor dependencies (like `IBranchRepository`) are resolved from DI automatically. No manual validator registration is ever needed in `ServiceCollectionExtensions.cs`.
- `ServiceFactory.cs` (`LvTest/Common/ServiceFactory.cs`) manually `new`s every service+dependency for tests — every constructor signature change to an existing service requires updating its factory method here.
- Build/test commands (run from `C:\Users\steve\OneDrive\Desktop\Steven\Projects\Construccion\Backend\LvTest`):
  - Build: `dotnet build`
  - Full test suite: `dotnet test LvTest/LvTest.csproj`
  - Single test: `dotnet test LvTest/LvTest.csproj --filter "FullyQualifiedName~<TestName>"`

---

## Task 1: Bridge `ProjectInventoryItem` to accept a `Product` (Warehouse→Project transfers)

**Why:** `InventoryMovement` moves a `Product` (Commercial catalog), but `ProjectInventoryItem` (Fase 7) is keyed by `MaterialId → MaterialCatalog`, a completely separate catalog. User decision (confirmed): extend `ProjectInventoryItem` with a nullable `ProductId` alongside a now-nullable `MaterialId`, enforce "exactly one populated" via CHECK constraint + filtered unique indexes — mirroring the `Invoice.InvoiceNumber` filtered-unique-index fix already applied this session.

**Files:**
- Modify: `LvDomain/Entities/Inventory/ProjectInventoryItem.cs`
- Modify: `LvInfrastructure/Persistence/Configurations/Inventory/ProjectInventoryItemConfiguration.cs`
- Modify: `LvApplication/Services/Inventory/IProjectInventoryItemRepository.cs`
- Modify: `LvInfrastructure/Repositories/Inventory/ProjectInventoryItemRepository.cs`
- Modify: `LvApplication/DTOs/Inventory/ProjectInventoryItemDto.cs`
- Modify: `LvApplication/Services/Inventory/MaterialTicketService.cs` (only the `GetInventoryAsync` mapping, line ~262)

**Interfaces:**
- Produces: `IProjectInventoryItemRepository.GetByProjectAndProductAsync(int projectId, int productId) : Task<ProjectInventoryItem?>` — consumed by Task 9.
- Produces: `ProjectInventoryItem.ProductId (int?)`, `ProjectInventoryItem.Product (Product?)` — consumed by Task 9.

- [ ] **Step 1: Update the entity**

`LvDomain/Entities/Inventory/ProjectInventoryItem.cs` — full replacement:

```csharp
using LvDomain.Common;
using LvDomain.Entities.Commercial;
using LvDomain.Entities.Materials;
using LvDomain.Entities.Projects;

namespace LvDomain.Entities.Inventory;

public class ProjectInventoryItem : BaseEntity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int? MaterialId { get; set; }
    public MaterialCatalog? Material { get; set; }

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public decimal CurrentQuantity { get; set; }
    public decimal ReferenceUnitCost { get; set; }
}
```

- [ ] **Step 2: Update the EF configuration**

`LvInfrastructure/Persistence/Configurations/Inventory/ProjectInventoryItemConfiguration.cs` — full replacement:

```csharp
using LvDomain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Inventory;

public class ProjectInventoryItemConfiguration : IEntityTypeConfiguration<ProjectInventoryItem>
{
    public void Configure(EntityTypeBuilder<ProjectInventoryItem> builder)
    {
        builder.ToTable("ProjectInventoryItems", t => t.HasCheckConstraint(
            "CK_ProjectInventoryItems_ExactlyOneCatalogReference",
            "([MaterialId] IS NOT NULL AND [ProductId] IS NULL) OR ([MaterialId] IS NULL AND [ProductId] IS NOT NULL)"));

        builder.HasKey(i => i.Id);

        builder.Property(i => i.CurrentQuantity).HasColumnType("decimal(18,2)");
        builder.Property(i => i.ReferenceUnitCost).HasColumnType("decimal(18,2)");

        builder.HasOne(i => i.Project)
            .WithMany()
            .HasForeignKey(i => i.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Material)
            .WithMany()
            .HasForeignKey(i => i.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(i => i.MaterialId);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(i => i.ProductId);

        builder.HasIndex(i => new { i.ProjectId, i.MaterialId }).IsUnique().HasFilter("[MaterialId] IS NOT NULL");
        builder.HasIndex(i => new { i.ProjectId, i.ProductId }).IsUnique().HasFilter("[ProductId] IS NOT NULL");
    }
}
```

- [ ] **Step 3: Add the new repository method**

`LvApplication/Services/Inventory/IProjectInventoryItemRepository.cs` — full replacement:

```csharp
using LvDomain.Entities.Inventory;

namespace LvApplication.Services.Inventory;

public interface IProjectInventoryItemRepository
{
    Task<ProjectInventoryItem?> GetByProjectAndMaterialAsync(int projectId, int materialId);
    Task<ProjectInventoryItem?> GetByProjectAndProductAsync(int projectId, int productId);
    Task<List<ProjectInventoryItem>> GetByProjectAsync(int projectId);
    Task AddAsync(ProjectInventoryItem item);
    Task UpdateAsync(ProjectInventoryItem item);
    Task<int> CountWithQuantityAsync(int projectId);
}
```

`LvInfrastructure/Repositories/Inventory/ProjectInventoryItemRepository.cs` — add this method (insert right after `GetByProjectAndMaterialAsync`):

```csharp
    public Task<ProjectInventoryItem?> GetByProjectAndProductAsync(int projectId, int productId) =>
        _context.ProjectInventoryItems.FirstOrDefaultAsync(i => i.ProjectId == projectId && i.ProductId == productId);
```

- [ ] **Step 4: Fix the DTO and its only mapping site (nullable `MaterialId` breaks the existing `int` assignment)**

`LvApplication/DTOs/Inventory/ProjectInventoryItemDto.cs` — full replacement:

```csharp
namespace LvApplication.DTOs.Inventory;

public class ProjectInventoryItemDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int? MaterialId { get; set; }
    public int? ProductId { get; set; }
    public decimal CurrentQuantity { get; set; }
    public decimal ReferenceUnitCost { get; set; }
}
```

`LvApplication/Services/Inventory/MaterialTicketService.cs` — in `GetInventoryAsync` (around line 258-265), replace:

```csharp
        return items.Select(i => new ProjectInventoryItemDto
        {
            Id = i.Id,
            ProjectId = i.ProjectId,
            MaterialId = i.MaterialId,
            CurrentQuantity = i.CurrentQuantity,
            ReferenceUnitCost = i.ReferenceUnitCost
        }).ToList();
```

with:

```csharp
        return items.Select(i => new ProjectInventoryItemDto
        {
            Id = i.Id,
            ProjectId = i.ProjectId,
            MaterialId = i.MaterialId,
            ProductId = i.ProductId,
            CurrentQuantity = i.CurrentQuantity,
            ReferenceUnitCost = i.ReferenceUnitCost
        }).ToList();
```

- [ ] **Step 5: Build and run the full suite — this task has no new test of its own (pure schema scaffolding consumed by Task 9); the deliverable is "compiles clean and nothing existing regresses"**

```bash
dotnet build
dotnet test LvTest/LvTest.csproj
```

Expected: build succeeds with 0 errors; all existing tests still pass (pay attention to `MaterialTicketServiceTests`, `SiteLogServiceTests`, `IncidentServiceTests` — they all touch `ProjectInventoryItem`/`MaterialId`).

- [ ] **Step 6: Mark step done** (no commit — not requested this session)

---

## Task 2: Worker → Branch assignment + Category/BranchType cross-validation

**Why:** Retrofit C. No Worker→Branch relationship exists in the codebase at all (confirmed by research — no `BranchId` on `Worker`, no cross-validation). User decision (confirmed): build it now as part of this phase, following the existing `WorkerCategoryTypeMap` validator-layer convention and the `CreateBudgetDtoValidator` precedent for injecting a repository into a FluentValidation validator for async cross-entity checks.

**Files:**
- Modify: `LvDomain/Entities/Workers/Worker.cs`
- Modify: `LvInfrastructure/Persistence/Configurations/Workers/WorkerConfiguration.cs`
- Modify: `LvApplication/DTOs/Workers/CreateWorkerDto.cs`
- Modify: `LvApplication/DTOs/Workers/UpdateWorkerDto.cs`
- Modify: `LvApplication/DTOs/Workers/WorkerResponseDto.cs`
- Modify: `LvApplication/Validators/Workers/CreateWorkerDtoValidator.cs`
- Modify: `LvApplication/Validators/Workers/UpdateWorkerDtoValidator.cs`
- Modify: `LvApplication/Services/Workers/WorkerService.cs`
- Modify: `LvTest/Common/ServiceFactory.cs` (`CreateWorkerService`)
- Test: `LvTest/Services/Workers/WorkerServiceTests.cs`

**Interfaces:**
- Consumes: `IBranchRepository.GetByIdAsync(int) : Task<Branch?>` (pre-existing, `LvApplication/Services/Branches/IBranchRepository.cs`), `Branch.BranchType` (pre-existing).
- Produces: `Worker.BranchId (int?)`, `CreateWorkerDto.BranchId (int?)`, `UpdateWorkerDto.BranchId (int?)`, `WorkerResponseDto.BranchId (int?)`.

- [ ] **Step 1: Write the failing tests**

Add to `LvTest/Services/Workers/WorkerServiceTests.cs`. First add `using LvDomain.Entities.Branches;` to the top of the file (alongside the existing usings), then add this private helper right after the class declaration (before the first `[Fact]`):

```csharp
    private static async Task<Branch> CreateBranchAsync(AppDbContext context, int operationsDirectorId, BranchType branchType)
    {
        var branch = new Branch
        {
            Name = "Bodega Central",
            City = "San Jose",
            Province = "San Jose",
            Status = BranchStatus.Active,
            BranchType = branchType,
            OperationsDirectorId = operationsDirectorId,
            CreatedAt = DateTime.UtcNow
        };
        context.Branches.Add(branch);
        await context.SaveChangesAsync();
        return branch;
    }
```

This also needs `using LvInfrastructure.Persistence;` for `AppDbContext` and `using LvDomain.Entities.Branches;` for `Branch` — check the existing top-of-file usings first; `using LvDomain.Enums;` is already present (it's what `WorkerCategory`/`WorkerType`/`ActiveStatus` already resolve through), so `BranchType`/`BranchStatus` need no new using, but `AppDbContext` and `Branch` do — add those two.

Then add these four tests (anywhere after the helper, e.g. right before the `GetByIdAsync_ExistingId_ReturnsWorker` test):

```csharp
    [Fact]
    public async Task CreateAsync_StorageCategoryAssignedToWarehouseBranch_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "wdir1@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var branch = await CreateBranchAsync(context, director.Id, BranchType.Warehouse);
        var service = ServiceFactory.CreateWorkerService(context);

        var result = await service.CreateAsync(new CreateWorkerDto
        {
            Name = "Bodeguero Uno",
            Category = WorkerCategory.Storage,
            Type = WorkerType.WarehouseKeeper,
            HourlyRate = 8,
            BranchId = branch.Id
        });

        result.BranchId.Should().Be(branch.Id);
    }

    [Fact]
    public async Task CreateAsync_NonStorageCategoryAssignedToWarehouseBranch_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "wdir2@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var branch = await CreateBranchAsync(context, director.Id, BranchType.Warehouse);
        var service = ServiceFactory.CreateWorkerService(context);

        var act = async () => await service.CreateAsync(new CreateWorkerDto
        {
            Name = "Vendedor Mal Asignado",
            Category = WorkerCategory.Commercial,
            Type = WorkerType.Salesperson,
            HourlyRate = 8,
            BranchId = branch.Id
        });

        var exception = await act.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Message.Should().Contain("Bodega deben tener categoría Almacenamiento");
    }

    [Fact]
    public async Task CreateAsync_AnyCategoryAssignedToCommercialBranch_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "wdir3@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var branch = await CreateBranchAsync(context, director.Id, BranchType.Commercial);
        var service = ServiceFactory.CreateWorkerService(context);

        var result = await service.CreateAsync(new CreateWorkerDto
        {
            Name = "Vendedor Comercio",
            Category = WorkerCategory.Commercial,
            Type = WorkerType.Salesperson,
            HourlyRate = 8,
            BranchId = branch.Id
        });

        result.BranchId.Should().Be(branch.Id);
    }

    [Fact]
    public async Task UpdateAsync_MismatchedCategoryForWarehouseBranch_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "wdir4@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var branch = await CreateBranchAsync(context, director.Id, BranchType.Warehouse);
        var service = ServiceFactory.CreateWorkerService(context);
        var created = await service.CreateAsync(new CreateWorkerDto
        {
            Name = "Bodeguero Dos",
            Category = WorkerCategory.Storage,
            Type = WorkerType.WarehouseKeeper,
            HourlyRate = 8,
            BranchId = branch.Id
        });

        var act = async () => await service.UpdateAsync(created.Id, new UpdateWorkerDto
        {
            Name = "Bodeguero Dos",
            Status = ActiveStatus.Active,
            Category = WorkerCategory.Commercial,
            Type = WorkerType.Salesperson,
            HourlyRate = 8,
            BranchId = branch.Id
        });

        var exception = await act.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Message.Should().Contain("Bodega deben tener categoría Almacenamiento");
    }
```

- [ ] **Step 2: Run the tests to verify they fail**

```bash
dotnet test LvTest/LvTest.csproj --filter "FullyQualifiedName~WorkerServiceTests"
```

Expected: compile error (`CreateWorkerDto` has no `BranchId`, `WorkerResponseDto` has no `BranchId`) — this is the correct "fails for the right reason" signal before the DTOs exist. Fix the DTOs/entity/config first (Steps 3-5), then re-run to confirm the two negative tests fail with the wrong-message/no-exception signal before Step 6's validator change.

- [ ] **Step 3: Add `BranchId` to the entity and configuration**

`LvDomain/Entities/Workers/Worker.cs` — full replacement:

```csharp
using LvDomain.Common;
using LvDomain.Entities.Branches;
using LvDomain.Enums;

namespace LvDomain.Entities.Workers;

public class Worker : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? PersonalId { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? Birthday { get; set; }
    public ActiveStatus Status { get; set; }
    public WorkerCategory Category { get; set; }
    public WorkerType Type { get; set; }
    public decimal HourlyRate { get; set; }

    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }
}
```

`LvInfrastructure/Persistence/Configurations/Workers/WorkerConfiguration.cs` — add before the final closing braces (after the existing `builder.HasIndex(w => w.PersonalId);` line):

```csharp
        builder.HasOne(w => w.Branch)
            .WithMany()
            .HasForeignKey(w => w.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(w => w.BranchId);
```

- [ ] **Step 4: Add `BranchId` to the three DTOs**

`LvApplication/DTOs/Workers/CreateWorkerDto.cs` — add `public int? BranchId { get; set; }` as the last property.
`LvApplication/DTOs/Workers/UpdateWorkerDto.cs` — add `public int? BranchId { get; set; }` as the last property.
`LvApplication/DTOs/Workers/WorkerResponseDto.cs` — add `public int? BranchId { get; set; }` as the last property.

- [ ] **Step 5: Wire `BranchId` through `WorkerService`**

`LvApplication/Services/Workers/WorkerService.cs` — in `CreateAsync`, add `BranchId = request.BranchId,` to the `new Worker { ... }` initializer (any position, e.g. right after `HourlyRate = request.HourlyRate,`). In `UpdateAsync`, add `worker.BranchId = request.BranchId;` right after `worker.HourlyRate = request.HourlyRate;`. In `MapToDto`, add `BranchId = worker.BranchId,` to the returned `WorkerResponseDto`.

- [ ] **Step 6: Add the cross-validation to both validators**

`LvApplication/Validators/Workers/CreateWorkerDtoValidator.cs` — full replacement:

```csharp
using FluentValidation;
using LvApplication.DTOs.Workers;
using LvApplication.Services.Branches;
using LvDomain.Enums;

namespace LvApplication.Validators.Workers;

public class CreateWorkerDtoValidator : AbstractValidator<CreateWorkerDto>
{
    public CreateWorkerDtoValidator(IBranchRepository branchRepository)
    {
        RuleFor(x => x.Name)
            .NotEmpty();

        RuleFor(x => x.Category)
            .IsInEnum();

        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.HourlyRate)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x)
            .Must(x => WorkerCategoryTypeMap.IsValidCombination(x.Category, x.Type))
            .WithMessage("El tipo de trabajador no corresponde a la categoría seleccionada")
            .WithName(nameof(CreateWorkerDto.Type));

        RuleFor(x => x.BranchId)
            .MustAsync(async (branchId, _) => await branchRepository.GetByIdAsync(branchId!.Value) is not null)
            .WithMessage("La sucursal indicada no existe")
            .When(x => x.BranchId.HasValue);

        RuleFor(x => x)
            .MustAsync(async (dto, _) =>
            {
                var branch = await branchRepository.GetByIdAsync(dto.BranchId!.Value);
                return branch is null || branch.BranchType != BranchType.Warehouse || dto.Category == WorkerCategory.Storage;
            })
            .WithMessage("Los trabajadores asignados a una sucursal de tipo Bodega deben tener categoría Almacenamiento")
            .WithName(nameof(CreateWorkerDto.Category))
            .When(x => x.BranchId.HasValue);
    }
}
```

`LvApplication/Validators/Workers/UpdateWorkerDtoValidator.cs` — full replacement:

```csharp
using FluentValidation;
using LvApplication.DTOs.Workers;
using LvApplication.Services.Branches;
using LvDomain.Enums;

namespace LvApplication.Validators.Workers;

public class UpdateWorkerDtoValidator : AbstractValidator<UpdateWorkerDto>
{
    public UpdateWorkerDtoValidator(IBranchRepository branchRepository)
    {
        RuleFor(x => x.Name)
            .NotEmpty();

        RuleFor(x => x.Status)
            .IsInEnum();

        RuleFor(x => x.Category)
            .IsInEnum();

        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.HourlyRate)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x)
            .Must(x => WorkerCategoryTypeMap.IsValidCombination(x.Category, x.Type))
            .WithMessage("El tipo de trabajador no corresponde a la categoría seleccionada")
            .WithName(nameof(UpdateWorkerDto.Type));

        RuleFor(x => x.BranchId)
            .MustAsync(async (branchId, _) => await branchRepository.GetByIdAsync(branchId!.Value) is not null)
            .WithMessage("La sucursal indicada no existe")
            .When(x => x.BranchId.HasValue);

        RuleFor(x => x)
            .MustAsync(async (dto, _) =>
            {
                var branch = await branchRepository.GetByIdAsync(dto.BranchId!.Value);
                return branch is null || branch.BranchType != BranchType.Warehouse || dto.Category == WorkerCategory.Storage;
            })
            .WithMessage("Los trabajadores asignados a una sucursal de tipo Bodega deben tener categoría Almacenamiento")
            .WithName(nameof(UpdateWorkerDto.Category))
            .When(x => x.BranchId.HasValue);
    }
}
```

- [ ] **Step 7: Update `ServiceFactory`**

`LvTest/Common/ServiceFactory.cs` — replace:

```csharp
    public static WorkerService CreateWorkerService(AppDbContext context) =>
        new(new WorkerRepository(context), new CreateWorkerDtoValidator(), new UpdateWorkerDtoValidator());
```

with:

```csharp
    public static WorkerService CreateWorkerService(AppDbContext context) =>
        new(new WorkerRepository(context), new CreateWorkerDtoValidator(new BranchRepository(context)), new UpdateWorkerDtoValidator(new BranchRepository(context)));
```

- [ ] **Step 8: Run the tests to verify they pass**

```bash
dotnet build
dotnet test LvTest/LvTest.csproj --filter "FullyQualifiedName~WorkerServiceTests"
```

Expected: all `WorkerServiceTests` pass (13 pre-existing + 4 new = 17), pristine output.

- [ ] **Step 9: Run the full suite to confirm no regressions, then mark step done**

```bash
dotnet test LvTest/LvTest.csproj
```

---

## Task 3: Retrofit A — `CashRegisterService.OpenAsync` and `InvoiceService.CreateAsync` require a Commercial branch

**Files:**
- Modify: `LvApplication/Services/Commercial/CashRegisterService.cs`
- Modify: `LvApplication/Services/Commercial/InvoiceService.cs`
- Modify: `LvTest/Common/ServiceFactory.cs` (`CreateCashRegisterService`, `CreateInvoiceService`)
- Test: `LvTest/Services/Commercial/CashRegisterServiceTests.cs`
- Test: `LvTest/Services/Commercial/InvoiceServiceTests.cs`

**Interfaces:**
- Consumes: `IBranchRepository.GetByIdAsync(int) : Task<Branch?>` (pre-existing).

- [ ] **Step 1: Write the failing tests**

Add to `LvTest/Services/Commercial/CashRegisterServiceTests.cs` (uses the file's existing `CreateBranchAsync` helper, which already parameterizes nothing — extend the call site inline by constructing a non-Commercial branch directly, since the helper is hardcoded to `BranchType.Commercial`; simplest is to add a second local branch inline in the test):

```csharp
    [Fact]
    public async Task OpenAsync_BranchNotCommercial_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "gmwh@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var warehouseBranch = new Branch
        {
            Name = "Bodega Central",
            City = "San Jose",
            Province = "San Jose",
            Status = BranchStatus.Active,
            BranchType = BranchType.Warehouse,
            OperationsDirectorId = user.Id,
            CreatedAt = DateTime.UtcNow
        };
        context.Branches.Add(warehouseBranch);
        await context.SaveChangesAsync();
        var service = ServiceFactory.CreateCashRegisterService(context);

        var act = () => service.OpenAsync(new OpenCashRegisterDto { BranchId = warehouseBranch.Id, OpeningBalance = 0m }, user.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }
```

Add to `LvTest/Services/Commercial/InvoiceServiceTests.cs` (needs a Warehouse-type branch, a cash register on it, and a product — reuse existing private helpers `CreateOpenCashRegisterAsync`/`CreateValidatedProductAsync`/`CreateInventoryAsync`, add a local branch inline since `CreateBranchAsync` in this file is also hardcoded to `BranchType.Commercial`):

```csharp
    [Fact]
    public async Task CreateAsync_BranchNotCommercial_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "invwh@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var warehouseBranch = new Branch
        {
            Name = "Bodega Central",
            City = "San Jose",
            Province = "San Jose",
            Status = BranchStatus.Active,
            BranchType = BranchType.Warehouse,
            OperationsDirectorId = user.Id,
            CreatedAt = DateTime.UtcNow
        };
        context.Branches.Add(warehouseBranch);
        await context.SaveChangesAsync();
        var register = await CreateOpenCashRegisterAsync(context, warehouseBranch.Id, user.Id);
        var product = await CreateValidatedProductAsync(context, user.Id, "SKU-WH-1");
        await CreateInventoryAsync(context, warehouseBranch.Id, product.Id, 10m);
        var service = ServiceFactory.CreateInvoiceService(context);

        var act = () => service.CreateAsync(new CreateInvoiceDto
        {
            BranchId = warehouseBranch.Id,
            CashRegisterId = register.Id,
            PaymentType = InvoicePaymentType.Credito,
            Details = new() { new InvoiceDetailLineDto { ProductId = product.Id, Quantity = 1 } }
        }, user.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }
```

- [ ] **Step 2: Run the tests to verify they fail**

```bash
dotnet test LvTest/LvTest.csproj --filter "FullyQualifiedName~OpenAsync_BranchNotCommercial_ThrowsValidationAppException|FullyQualifiedName~CreateAsync_BranchNotCommercial_ThrowsValidationAppException"
```

Expected: both FAIL — `OpenAsync`/`CreateAsync` currently succeed for any branch type (no exception thrown).

- [ ] **Step 3: Add the check to `CashRegisterService`**

`LvApplication/Services/Commercial/CashRegisterService.cs` — replace the constructor and field block:

```csharp
    private readonly ICashRegisterRepository _cashRegisterRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IValidator<OpenCashRegisterDto> _openValidator;
    private readonly IValidator<CloseCashRegisterDto> _closeValidator;

    public CashRegisterService(
        ICashRegisterRepository cashRegisterRepository,
        IBranchRepository branchRepository,
        IValidator<OpenCashRegisterDto> openValidator,
        IValidator<CloseCashRegisterDto> closeValidator)
    {
        _cashRegisterRepository = cashRegisterRepository;
        _branchRepository = branchRepository;
        _openValidator = openValidator;
        _closeValidator = closeValidator;
    }
```

and in `OpenAsync`, right after `await _openValidator.ValidateAndThrowAppExceptionAsync(request);`, insert:

```csharp
        var branch = await _branchRepository.GetByIdAsync(request.BranchId)
            ?? throw new NotFoundException($"Branch {request.BranchId} not found.");

        if (branch.BranchType != BranchType.Commercial)
        {
            throw new ValidationAppException("Solo se puede abrir una caja en una sucursal de tipo Comercio.");
        }
```

Add `using LvApplication.Services.Branches;` to the top of the file.

- [ ] **Step 4: Add the check to `InvoiceService`**

`LvApplication/Services/Commercial/InvoiceService.cs` — replace the constructor and field block:

```csharp
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IBranchInventoryRepository _inventoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICashRegisterRepository _cashRegisterRepository;
    private readonly IValidator<CreateInvoiceDto> _createValidator;
    private readonly IValidator<UpdateInvoiceDraftDto> _updateDraftValidator;
    private readonly IValidator<IssueInvoiceDto> _issueValidator;
    private readonly IValidator<CreateInvoicePaymentDto> _paymentValidator;

    public InvoiceService(
        IInvoiceRepository invoiceRepository,
        IBranchRepository branchRepository,
        IBranchInventoryRepository inventoryRepository,
        IProductRepository productRepository,
        ICashRegisterRepository cashRegisterRepository,
        IValidator<CreateInvoiceDto> createValidator,
        IValidator<UpdateInvoiceDraftDto> updateDraftValidator,
        IValidator<IssueInvoiceDto> issueValidator,
        IValidator<CreateInvoicePaymentDto> paymentValidator)
    {
        _invoiceRepository = invoiceRepository;
        _branchRepository = branchRepository;
        _inventoryRepository = inventoryRepository;
        _productRepository = productRepository;
        _cashRegisterRepository = cashRegisterRepository;
        _createValidator = createValidator;
        _updateDraftValidator = updateDraftValidator;
        _issueValidator = issueValidator;
        _paymentValidator = paymentValidator;
    }
```

and in `CreateAsync`, right after `await _createValidator.ValidateAndThrowAppExceptionAsync(request);`, insert:

```csharp
        var branch = await _branchRepository.GetByIdAsync(request.BranchId)
            ?? throw new NotFoundException($"Branch {request.BranchId} not found.");

        if (branch.BranchType != BranchType.Commercial)
        {
            throw new ValidationAppException("Solo se puede facturar en una sucursal de tipo Comercio.");
        }
```

Add `using LvApplication.Services.Branches;` to the top of the file.

- [ ] **Step 5: Update `ServiceFactory`**

`LvTest/Common/ServiceFactory.cs` — replace:

```csharp
    public static CashRegisterService CreateCashRegisterService(AppDbContext context) =>
        new(
            new CashRegisterRepository(context),
            new OpenCashRegisterDtoValidator(),
            new CloseCashRegisterDtoValidator());

    public static InvoiceService CreateInvoiceService(AppDbContext context) =>
        new(
            new InvoiceRepository(context),
            new BranchInventoryRepository(context),
            new ProductRepository(context),
            new CashRegisterRepository(context),
            new CreateInvoiceDtoValidator(),
            new UpdateInvoiceDraftDtoValidator(),
            new IssueInvoiceDtoValidator(),
            new CreateInvoicePaymentDtoValidator());
```

with:

```csharp
    public static CashRegisterService CreateCashRegisterService(AppDbContext context) =>
        new(
            new CashRegisterRepository(context),
            new BranchRepository(context),
            new OpenCashRegisterDtoValidator(),
            new CloseCashRegisterDtoValidator());

    public static InvoiceService CreateInvoiceService(AppDbContext context) =>
        new(
            new InvoiceRepository(context),
            new BranchRepository(context),
            new BranchInventoryRepository(context),
            new ProductRepository(context),
            new CashRegisterRepository(context),
            new CreateInvoiceDtoValidator(),
            new UpdateInvoiceDraftDtoValidator(),
            new IssueInvoiceDtoValidator(),
            new CreateInvoicePaymentDtoValidator());
```

- [ ] **Step 6: Run the tests to verify they pass, then run the full suite**

```bash
dotnet build
dotnet test LvTest/LvTest.csproj --filter "FullyQualifiedName~CashRegisterServiceTests|FullyQualifiedName~InvoiceServiceTests"
dotnet test LvTest/LvTest.csproj
```

Expected: both new tests pass, all pre-existing `CashRegisterServiceTests`/`InvoiceServiceTests` still pass (their branches are already `BranchType.Commercial`), full suite green.

- [ ] **Step 7: Mark step done**

---

## Task 4: Retrofit B — `ProductIncorporationTicketService.CreateAsync` requires a Commercial or Warehouse branch

**Files:**
- Modify: `LvApplication/Services/Commercial/ProductIncorporationTicketService.cs`
- Modify: `LvTest/Common/ServiceFactory.cs` (`CreateProductIncorporationTicketService`)
- Test: `LvTest/Services/Commercial/ProductIncorporationTicketServiceTests.cs`

**Interfaces:**
- Consumes: `IBranchRepository.GetByIdAsync(int) : Task<Branch?>` (pre-existing).

- [ ] **Step 1: Write the failing tests**

Add to `LvTest/Services/Commercial/ProductIncorporationTicketServiceTests.cs`:

```csharp
    [Fact]
    public async Task CreateAsync_BranchNotCommercialOrWarehouse_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "pitoffice@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var officeBranch = new Branch
        {
            Name = "Oficina Central",
            City = "San Jose",
            Province = "San Jose",
            Status = BranchStatus.Active,
            BranchType = BranchType.Office,
            OperationsDirectorId = user.Id,
            CreatedAt = DateTime.UtcNow
        };
        context.Branches.Add(officeBranch);
        await context.SaveChangesAsync();
        var supplier = await CreateSupplierAsync(context);
        var product = await CreateProductAsync(context, user.Id);
        var service = ServiceFactory.CreateProductIncorporationTicketService(context);

        var act = () => service.CreateAsync(BuildDto(officeBranch.Id, product.Id, supplier.Id), user.Id, new[] { "GeneralManager" });

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task CreateAsync_WarehouseBranch_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "pitwh@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var warehouseBranch = new Branch
        {
            Name = "Bodega Central",
            City = "San Jose",
            Province = "San Jose",
            Status = BranchStatus.Active,
            BranchType = BranchType.Warehouse,
            OperationsDirectorId = user.Id,
            CreatedAt = DateTime.UtcNow
        };
        context.Branches.Add(warehouseBranch);
        await context.SaveChangesAsync();
        var supplier = await CreateSupplierAsync(context);
        var product = await CreateProductAsync(context, user.Id);
        var service = ServiceFactory.CreateProductIncorporationTicketService(context);

        var result = await service.CreateAsync(BuildDto(warehouseBranch.Id, product.Id, supplier.Id), user.Id, new[] { "GeneralManager" });

        result.Status.Should().Be(ProductIncorporationTicketStatus.Validated);
    }
```

- [ ] **Step 2: Run the tests to verify they fail (or pass for the wrong reason)**

```bash
dotnet test LvTest/LvTest.csproj --filter "FullyQualifiedName~CreateAsync_BranchNotCommercialOrWarehouse_ThrowsValidationAppException"
```

Expected: FAILS — no exception thrown for an Office branch today. (The `WarehouseBranch_Succeeds` test should already pass — it's a regression guard, not a red test; that's fine, note it in the plan same as the InventoryNumber test earlier this session.)

- [ ] **Step 3: Add the check**

`LvApplication/Services/Commercial/ProductIncorporationTicketService.cs` — replace the field/constructor block:

```csharp
    private readonly IProductIncorporationTicketRepository _ticketRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IBranchInventoryRepository _inventoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly IValidator<CreateProductIncorporationTicketDto> _createValidator;

    public ProductIncorporationTicketService(
        IProductIncorporationTicketRepository ticketRepository,
        IBranchRepository branchRepository,
        IBranchInventoryRepository inventoryRepository,
        IProductRepository productRepository,
        IValidator<CreateProductIncorporationTicketDto> createValidator)
    {
        _ticketRepository = ticketRepository;
        _branchRepository = branchRepository;
        _inventoryRepository = inventoryRepository;
        _productRepository = productRepository;
        _createValidator = createValidator;
    }
```

and in `CreateAsync`, right after `await _createValidator.ValidateAndThrowAppExceptionAsync(request);`, insert:

```csharp
        var branch = await _branchRepository.GetByIdAsync(request.BranchId)
            ?? throw new NotFoundException($"Branch {request.BranchId} not found.");

        if (branch.BranchType is not (BranchType.Commercial or BranchType.Warehouse))
        {
            throw new ValidationAppException("La sucursal debe ser de tipo Comercio o Bodega para incorporar productos.");
        }
```

Add `using LvApplication.Services.Branches;` to the top of the file.

- [ ] **Step 4: Update `ServiceFactory`**

`LvTest/Common/ServiceFactory.cs` — replace:

```csharp
    public static ProductIncorporationTicketService CreateProductIncorporationTicketService(AppDbContext context) =>
        new(
            new ProductIncorporationTicketRepository(context),
            new BranchInventoryRepository(context),
            new ProductRepository(context),
            new CreateProductIncorporationTicketDtoValidator());
```

with:

```csharp
    public static ProductIncorporationTicketService CreateProductIncorporationTicketService(AppDbContext context) =>
        new(
            new ProductIncorporationTicketRepository(context),
            new BranchRepository(context),
            new BranchInventoryRepository(context),
            new ProductRepository(context),
            new CreateProductIncorporationTicketDtoValidator());
```

- [ ] **Step 5: Run the tests to verify they pass, then run the full suite**

```bash
dotnet build
dotnet test LvTest/LvTest.csproj --filter "FullyQualifiedName~ProductIncorporationTicketServiceTests"
dotnet test LvTest/LvTest.csproj
```

Expected: all pass, including the 8 pre-existing tests (their branches are already `BranchType.Commercial`).

- [ ] **Step 6: Mark step done**

---

## Task 5: `InventoryMovement` domain + persistence

**Files:**
- Create: `LvDomain/Enums/InventoryMovementStatus.cs`
- Create: `LvDomain/Entities/Warehouse/InventoryMovement.cs`
- Create: `LvInfrastructure/Persistence/Configurations/Warehouse/InventoryMovementConfiguration.cs`
- Modify: `LvInfrastructure/Persistence/AppDbContext.cs`

**Interfaces:**
- Produces: `InventoryMovement` entity with `Id, OriginBranchId, DestinationBranchId?, DestinationProjectId?, ProductId, Quantity, Status, SentByUserId, SentDate, ValidatedByUserId?, ValidatedDate?` — consumed by every later task in this plan.
- Produces: `InventoryMovementStatus { Sent, Accepted, Denied }`.

- [ ] **Step 1: Create the enum**

`LvDomain/Enums/InventoryMovementStatus.cs`:

```csharp
namespace LvDomain.Enums;

public enum InventoryMovementStatus
{
    Sent,
    Accepted,
    Denied
}
```

- [ ] **Step 2: Create the entity**

`LvDomain/Entities/Warehouse/InventoryMovement.cs`:

```csharp
using LvDomain.Common;
using LvDomain.Entities.Auth;
using LvDomain.Entities.Branches;
using LvDomain.Entities.Commercial;
using LvDomain.Entities.Projects;
using LvDomain.Enums;

namespace LvDomain.Entities.Warehouse;

public class InventoryMovement : BaseEntity
{
    public int OriginBranchId { get; set; }
    public Branch OriginBranch { get; set; } = null!;

    public int? DestinationBranchId { get; set; }
    public Branch? DestinationBranch { get; set; }

    public int? DestinationProjectId { get; set; }
    public Project? DestinationProject { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal Quantity { get; set; }

    public InventoryMovementStatus Status { get; set; }

    public int SentByUserId { get; set; }
    public User SentByUser { get; set; } = null!;
    public DateTime SentDate { get; set; }

    public int? ValidatedByUserId { get; set; }
    public User? ValidatedByUser { get; set; }
    public DateTime? ValidatedDate { get; set; }
}
```

- [ ] **Step 3: Create the EF configuration**

`LvInfrastructure/Persistence/Configurations/Warehouse/InventoryMovementConfiguration.cs`:

```csharp
using LvDomain.Entities.Warehouse;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Warehouse;

public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("InventoryMovements", t => t.HasCheckConstraint(
            "CK_InventoryMovements_ExactlyOneDestination",
            "([DestinationBranchId] IS NOT NULL AND [DestinationProjectId] IS NULL) OR ([DestinationBranchId] IS NULL AND [DestinationProjectId] IS NOT NULL)"));

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Quantity).HasColumnType("decimal(18,2)");
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne(m => m.OriginBranch).WithMany().HasForeignKey(m => m.OriginBranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(m => m.OriginBranchId);

        builder.HasOne(m => m.DestinationBranch).WithMany().HasForeignKey(m => m.DestinationBranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(m => m.DestinationBranchId);

        builder.HasOne(m => m.DestinationProject).WithMany().HasForeignKey(m => m.DestinationProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(m => m.DestinationProjectId);

        builder.HasOne(m => m.Product).WithMany().HasForeignKey(m => m.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(m => m.ProductId);

        builder.HasOne(m => m.SentByUser).WithMany().HasForeignKey(m => m.SentByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.ValidatedByUser).WithMany().HasForeignKey(m => m.ValidatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
```

- [ ] **Step 4: Register the `DbSet`**

`LvInfrastructure/Persistence/AppDbContext.cs` — add `using LvDomain.Entities.Warehouse;` to the usings, and add this line right after the existing Commercial `DbSet`s (after `public DbSet<InvoicePayment> InvoicePayments => Set<InvoicePayment>();`):

```csharp
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
```

`modelBuilder.ApplyConfigurationsFromAssembly(...)` already auto-discovers the new configuration class — no further registration needed.

- [ ] **Step 5: Build to confirm it compiles — no new test yet (pure scaffolding consumed by Tasks 8-10)**

```bash
dotnet build
```

Expected: 0 errors.

- [ ] **Step 6: Mark step done**

---

## Task 6: `InventoryMovement` DTOs + validator

**Files:**
- Create: `LvApplication/DTOs/Warehouse/InventoryMovementDto.cs`
- Create: `LvApplication/DTOs/Warehouse/CreateInventoryMovementDto.cs`
- Create: `LvApplication/DTOs/Warehouse/ValidateInventoryMovementDto.cs`
- Create: `LvApplication/Validators/Warehouse/CreateInventoryMovementDtoValidator.cs`

**Interfaces:**
- Produces: `CreateInventoryMovementDto { OriginBranchId, DestinationBranchId?, DestinationProjectId?, ProductId, Quantity }`, `ValidateInventoryMovementDto { Approve }`, `InventoryMovementDto` (full read model) — consumed by Tasks 8-11.

- [ ] **Step 1: Create the DTOs**

`LvApplication/DTOs/Warehouse/InventoryMovementDto.cs`:

```csharp
using LvDomain.Enums;

namespace LvApplication.DTOs.Warehouse;

public class InventoryMovementDto
{
    public int Id { get; set; }
    public int OriginBranchId { get; set; }
    public int? DestinationBranchId { get; set; }
    public int? DestinationProjectId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public InventoryMovementStatus Status { get; set; }
    public int SentByUserId { get; set; }
    public DateTime SentDate { get; set; }
    public int? ValidatedByUserId { get; set; }
    public DateTime? ValidatedDate { get; set; }
}
```

`LvApplication/DTOs/Warehouse/CreateInventoryMovementDto.cs`:

```csharp
namespace LvApplication.DTOs.Warehouse;

public class CreateInventoryMovementDto
{
    public int OriginBranchId { get; set; }
    public int? DestinationBranchId { get; set; }
    public int? DestinationProjectId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
}
```

`LvApplication/DTOs/Warehouse/ValidateInventoryMovementDto.cs`:

```csharp
namespace LvApplication.DTOs.Warehouse;

public class ValidateInventoryMovementDto
{
    public bool Approve { get; set; }
}
```

- [ ] **Step 2: Create the validator**

`LvApplication/Validators/Warehouse/CreateInventoryMovementDtoValidator.cs`:

```csharp
using FluentValidation;
using LvApplication.DTOs.Warehouse;

namespace LvApplication.Validators.Warehouse;

public class CreateInventoryMovementDtoValidator : AbstractValidator<CreateInventoryMovementDto>
{
    public CreateInventoryMovementDtoValidator()
    {
        RuleFor(x => x.OriginBranchId).GreaterThan(0);
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);

        RuleFor(x => x.DestinationBranchId).GreaterThan(0).When(x => x.DestinationBranchId.HasValue);
        RuleFor(x => x.DestinationProjectId).GreaterThan(0).When(x => x.DestinationProjectId.HasValue);

        RuleFor(x => x)
            .Must(x => (x.DestinationBranchId.HasValue && !x.DestinationProjectId.HasValue) ||
                       (!x.DestinationBranchId.HasValue && x.DestinationProjectId.HasValue))
            .WithMessage("Debe indicar exactamente un destino: sucursal o proyecto, no ambos ni ninguno.")
            .WithName(nameof(CreateInventoryMovementDto.DestinationBranchId));
    }
}
```

- [ ] **Step 3: Build to confirm it compiles**

```bash
dotnet build
```

- [ ] **Step 4: Mark step done** (no runnable test yet — this validator is exercised end-to-end in Task 8)

---

## Task 7: `InventoryMovement` repository

**Files:**
- Create: `LvApplication/Services/Warehouse/IInventoryMovementRepository.cs`
- Create: `LvInfrastructure/Repositories/Warehouse/InventoryMovementRepository.cs`

**Interfaces:**
- Produces: `IInventoryMovementRepository { GetByIdAsync(int), AddAsync(InventoryMovement), UpdateAsync(InventoryMovement), DeleteAsync(InventoryMovement), GetPagedByOriginBranchAsync(int,int,int) }` — consumed by Task 8-10.

- [ ] **Step 1: Create the interface**

`LvApplication/Services/Warehouse/IInventoryMovementRepository.cs`:

```csharp
using LvDomain.Entities.Warehouse;

namespace LvApplication.Services.Warehouse;

public interface IInventoryMovementRepository
{
    Task<InventoryMovement?> GetByIdAsync(int id);
    Task AddAsync(InventoryMovement movement);
    Task UpdateAsync(InventoryMovement movement);
    Task DeleteAsync(InventoryMovement movement);
    Task<(List<InventoryMovement> Items, int TotalCount)> GetPagedByOriginBranchAsync(int branchId, int pageNumber, int pageSize);
}
```

- [ ] **Step 2: Create the implementation**

`LvInfrastructure/Repositories/Warehouse/InventoryMovementRepository.cs`:

```csharp
using LvApplication.Services.Warehouse;
using LvDomain.Entities.Warehouse;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LvInfrastructure.Repositories.Warehouse;

public class InventoryMovementRepository : IInventoryMovementRepository
{
    private readonly AppDbContext _context;

    public InventoryMovementRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<InventoryMovement?> GetByIdAsync(int id) =>
        await _context.InventoryMovements.FirstOrDefaultAsync(m => m.Id == id);

    public async Task AddAsync(InventoryMovement movement)
    {
        _context.InventoryMovements.Add(movement);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(InventoryMovement movement)
    {
        _context.InventoryMovements.Update(movement);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(InventoryMovement movement)
    {
        _context.InventoryMovements.Remove(movement);
        await _context.SaveChangesAsync();
    }

    public async Task<(List<InventoryMovement> Items, int TotalCount)> GetPagedByOriginBranchAsync(int branchId, int pageNumber, int pageSize)
    {
        var query = _context.InventoryMovements.Where(m => m.OriginBranchId == branchId);
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.SentDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
```

- [ ] **Step 3: Build to confirm it compiles**

```bash
dotnet build
```

- [ ] **Step 4: Mark step done**

---

## Task 8: `InventoryMovementService` — `CreateAsync`

**Files:**
- Create: `LvApplication/Services/Warehouse/IInventoryMovementService.cs`
- Create: `LvApplication/Services/Warehouse/InventoryMovementService.cs` (this task implements `CreateAsync` + `MapToDto`; Tasks 9-10 add the remaining methods to this same file)
- Modify: `LvTest/Common/ServiceFactory.cs` (add `CreateInventoryMovementService`)
- Test: Create `LvTest/Services/Warehouse/InventoryMovementServiceTests.cs`

**Interfaces:**
- Consumes: `IInventoryMovementRepository` (Task 7), `IBranchRepository.GetByIdAsync` (pre-existing), `IProjectRepository.GetByIdAsync` (pre-existing, `LvApplication/Services/Projects/IProjectRepository.cs`), `IProductRepository.GetByIdAsync` (pre-existing).
- Produces: `InventoryMovementService.CreateAsync(CreateInventoryMovementDto request, int sentByUserId) : Task<InventoryMovementDto>` — consumed by Task 11 (controller) and Tasks 9-10's tests.

- [ ] **Step 1: Write the failing tests**

Create `LvTest/Services/Warehouse/InventoryMovementServiceTests.cs`:

```csharp
using FluentAssertions;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Projects;
using LvApplication.DTOs.Warehouse;
using LvDomain.Entities.Branches;
using LvDomain.Entities.Budgets;
using LvDomain.Entities.Commercial;
using LvDomain.Entities.Customers;
using LvDomain.Entities.Offers;
using LvDomain.Enums;
using LvInfrastructure.Persistence;
using LvTest.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LvTest.Services.Warehouse;

public class InventoryMovementServiceTests
{
    private static async Task<Branch> CreateBranchAsync(AppDbContext context, int operationsDirectorId, BranchType branchType, string name = "Sucursal Test")
    {
        var branch = new Branch
        {
            Name = name,
            City = "San Jose",
            Province = "San Jose",
            Status = BranchStatus.Active,
            BranchType = branchType,
            OperationsDirectorId = operationsDirectorId,
            CreatedAt = DateTime.UtcNow
        };
        context.Branches.Add(branch);
        await context.SaveChangesAsync();
        return branch;
    }

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

    private static async Task<ProjectDto> CreateActiveProjectAsync(AppDbContext context)
    {
        var director = await TestUserFactory.CreateAsync(context, $"director-{Guid.NewGuid():N}@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var manager = await TestUserFactory.CreateAsync(context, $"gm-{Guid.NewGuid():N}@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var projectBranch = await CreateBranchAsync(context, director.Id, BranchType.Office, "Oficina Proyecto");
        var budget = await CreateApprovedBudgetAsync(context, customer.Id, projectBranch.Id, manager.Id);
        var offer = await CreateAcceptedOfferAsync(context, budget.Id, customer.Id, manager.Id);

        var projectService = ServiceFactory.CreateProjectService(context);
        return await projectService.CreateProjectAsync(new CreateProjectDto
        {
            OfferId = offer.Id,
            BranchId = projectBranch.Id,
            StartDate = new DateTime(2026, 3, 1)
        }, manager.Id);
    }

    private static async Task<Product> CreateValidatedProductAsync(AppDbContext context, int createdByUserId, string sku, decimal unitCost = 700m)
    {
        var product = new Product
        {
            Name = "Producto " + sku,
            Sku = sku,
            UnitPrice = 1000m,
            UnitCost = unitCost,
            Status = ProductStatus.Validated,
            ActiveStatus = true,
            CreatedByUserId = createdByUserId,
            ValidatedByUserId = createdByUserId,
            ValidatedDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product;
    }

    private static async Task<BranchInventory> CreateInventoryAsync(AppDbContext context, int branchId, int productId, decimal quantity)
    {
        var inventory = new BranchInventory { BranchId = branchId, ProductId = productId, Quantity = quantity, MinimumStock = 0, CreatedAt = DateTime.UtcNow };
        context.BranchInventories.Add(inventory);
        await context.SaveChangesAsync();
        return inventory;
    }

    private static CreateInventoryMovementDto BuildDtoToBranch(int originBranchId, int destinationBranchId, int productId, decimal quantity = 5) => new()
    {
        OriginBranchId = originBranchId,
        DestinationBranchId = destinationBranchId,
        ProductId = productId,
        Quantity = quantity
    };

    private static CreateInventoryMovementDto BuildDtoToProject(int originBranchId, int destinationProjectId, int productId, decimal quantity = 5) => new()
    {
        OriginBranchId = originBranchId,
        DestinationProjectId = destinationProjectId,
        ProductId = productId,
        Quantity = quantity
    };

    [Fact]
    public async Task CreateAsync_ValidDestinationBranch_CreatesSentMovement()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "im1@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var warehouse = await CreateBranchAsync(context, user.Id, BranchType.Warehouse, "Bodega");
        var commerce = await CreateBranchAsync(context, user.Id, BranchType.Commercial, "Comercio");
        var product = await CreateValidatedProductAsync(context, user.Id, "SKU-IM-1");
        await CreateInventoryAsync(context, warehouse.Id, product.Id, 20m);
        var service = ServiceFactory.CreateInventoryMovementService(context);

        var result = await service.CreateAsync(BuildDtoToBranch(warehouse.Id, commerce.Id, product.Id, 5), user.Id);

        result.Status.Should().Be(InventoryMovementStatus.Sent);
        result.ValidatedByUserId.Should().BeNull();
        var origin = await context.BranchInventories.FirstAsync(i => i.BranchId == warehouse.Id && i.ProductId == product.Id);
        origin.Quantity.Should().Be(20m, "creating a movement must not touch inventory yet");
    }

    [Fact]
    public async Task CreateAsync_ValidDestinationProject_CreatesSentMovement()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "im2@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var warehouse = await CreateBranchAsync(context, user.Id, BranchType.Warehouse, "Bodega");
        var product = await CreateValidatedProductAsync(context, user.Id, "SKU-IM-2");
        await CreateInventoryAsync(context, warehouse.Id, product.Id, 20m);
        var project = await CreateActiveProjectAsync(context);
        var service = ServiceFactory.CreateInventoryMovementService(context);

        var result = await service.CreateAsync(BuildDtoToProject(warehouse.Id, project.Id, product.Id, 5), user.Id);

        result.Status.Should().Be(InventoryMovementStatus.Sent);
    }

    [Fact]
    public async Task CreateAsync_OriginNotWarehouse_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "im3@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var commerceOrigin = await CreateBranchAsync(context, user.Id, BranchType.Commercial, "Comercio Origen");
        var commerceDestination = await CreateBranchAsync(context, user.Id, BranchType.Commercial, "Comercio Destino");
        var product = await CreateValidatedProductAsync(context, user.Id, "SKU-IM-3");
        var service = ServiceFactory.CreateInventoryMovementService(context);

        var act = () => service.CreateAsync(BuildDtoToBranch(commerceOrigin.Id, commerceDestination.Id, product.Id), user.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task CreateAsync_DestinationBranchNotCommercial_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "im4@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var warehouseOrigin = await CreateBranchAsync(context, user.Id, BranchType.Warehouse, "Bodega Origen");
        var warehouseDestination = await CreateBranchAsync(context, user.Id, BranchType.Warehouse, "Bodega Destino");
        var product = await CreateValidatedProductAsync(context, user.Id, "SKU-IM-4");
        var service = ServiceFactory.CreateInventoryMovementService(context);

        var act = () => service.CreateAsync(BuildDtoToBranch(warehouseOrigin.Id, warehouseDestination.Id, product.Id), user.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task CreateAsync_NoDestination_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "im5@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var warehouse = await CreateBranchAsync(context, user.Id, BranchType.Warehouse, "Bodega");
        var product = await CreateValidatedProductAsync(context, user.Id, "SKU-IM-5");
        var service = ServiceFactory.CreateInventoryMovementService(context);

        var act = () => service.CreateAsync(new CreateInventoryMovementDto
        {
            OriginBranchId = warehouse.Id,
            ProductId = product.Id,
            Quantity = 5
        }, user.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task CreateAsync_BothDestinations_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "im6@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var warehouse = await CreateBranchAsync(context, user.Id, BranchType.Warehouse, "Bodega");
        var commerce = await CreateBranchAsync(context, user.Id, BranchType.Commercial, "Comercio");
        var product = await CreateValidatedProductAsync(context, user.Id, "SKU-IM-6");
        var project = await CreateActiveProjectAsync(context);
        var service = ServiceFactory.CreateInventoryMovementService(context);

        var act = () => service.CreateAsync(new CreateInventoryMovementDto
        {
            OriginBranchId = warehouse.Id,
            DestinationBranchId = commerce.Id,
            DestinationProjectId = project.Id,
            ProductId = product.Id,
            Quantity = 5
        }, user.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }
}
```

`CreateActiveProjectAsync` (defined above in this same file) creates its own `OperationsDirector`/`GeneralManager` users, an approved `Budget`, and an accepted `Offer` internally — it does not need the ambient `user` variable, since `Project` requires its own `OfferId`/`BudgetId`/`CustomerId`/`BranchId` chain regardless of who is performing the `InventoryMovement` operation.

- [ ] **Step 2: Add the `CreateInventoryMovementService` factory method (needed before Step 1's tests can even compile)**

`LvTest/Common/ServiceFactory.cs` — add (needs `using LvApplication.Services.Warehouse;`, `using LvInfrastructure.Repositories.Warehouse;`, `using LvApplication.Validators.Warehouse;` at the top):

```csharp
    public static InventoryMovementService CreateInventoryMovementService(AppDbContext context) =>
        new(
            new InventoryMovementRepository(context),
            new BranchRepository(context),
            new ProjectRepository(context),
            new ProductRepository(context),
            new BranchInventoryRepository(context),
            new ProjectInventoryItemRepository(context),
            new CreateInventoryMovementDtoValidator());
```

- [ ] **Step 3: Run the new tests to verify they fail**

```bash
dotnet test LvTest/LvTest.csproj --filter "FullyQualifiedName~InventoryMovementServiceTests"
```

Expected: compile error (`InventoryMovementService` doesn't exist yet) — correct "fails for the right reason" before Step 4.

- [ ] **Step 4: Implement `IInventoryMovementService` and `InventoryMovementService.CreateAsync`**

`LvApplication/Services/Warehouse/IInventoryMovementService.cs`:

```csharp
using LvApplication.Common;
using LvApplication.DTOs.Warehouse;

namespace LvApplication.Services.Warehouse;

public interface IInventoryMovementService
{
    Task<InventoryMovementDto> CreateAsync(CreateInventoryMovementDto request, int sentByUserId);
    Task<InventoryMovementDto> ValidateAsync(int id, bool approve, int actingUserId, IEnumerable<string> actingUserRoles);
    Task DeleteAsync(int id, IEnumerable<string> actingUserRoles);
    Task<InventoryMovementDto> GetByIdAsync(int id);
    Task<PagedResult<InventoryMovementDto>> GetAllByOriginBranchAsync(int branchId, int pageNumber, int pageSize);
}
```

`LvApplication/Services/Warehouse/InventoryMovementService.cs` (this is the file Tasks 9-10 will extend — `ValidateAsync`/`DeleteAsync`/`GetByIdAsync`/`GetAllByOriginBranchAsync` are stubbed with `throw new NotImplementedException()` here and replaced for real in Tasks 9-10, so the file compiles against the interface at every step):

```csharp
using FluentValidation;
using LvApplication.Common;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Warehouse;
using LvApplication.Services.Branches;
using LvApplication.Services.Commercial;
using LvApplication.Services.Inventory;
using LvApplication.Services.Projects;
using LvDomain.Entities.Warehouse;
using LvDomain.Enums;

namespace LvApplication.Services.Warehouse;

public class InventoryMovementService : IInventoryMovementService
{
    private readonly IInventoryMovementRepository _movementRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProductRepository _productRepository;
    private readonly IBranchInventoryRepository _branchInventoryRepository;
    private readonly IProjectInventoryItemRepository _projectInventoryItemRepository;
    private readonly IValidator<CreateInventoryMovementDto> _createValidator;

    public InventoryMovementService(
        IInventoryMovementRepository movementRepository,
        IBranchRepository branchRepository,
        IProjectRepository projectRepository,
        IProductRepository productRepository,
        IBranchInventoryRepository branchInventoryRepository,
        IProjectInventoryItemRepository projectInventoryItemRepository,
        IValidator<CreateInventoryMovementDto> createValidator)
    {
        _movementRepository = movementRepository;
        _branchRepository = branchRepository;
        _projectRepository = projectRepository;
        _productRepository = productRepository;
        _branchInventoryRepository = branchInventoryRepository;
        _projectInventoryItemRepository = projectInventoryItemRepository;
        _createValidator = createValidator;
    }

    public async Task<InventoryMovementDto> CreateAsync(CreateInventoryMovementDto request, int sentByUserId)
    {
        await _createValidator.ValidateAndThrowAppExceptionAsync(request);

        var originBranch = await _branchRepository.GetByIdAsync(request.OriginBranchId)
            ?? throw new NotFoundException($"Branch {request.OriginBranchId} not found.");

        if (originBranch.BranchType != BranchType.Warehouse)
        {
            throw new ValidationAppException("La sucursal de origen de un movimiento de inventario debe ser de tipo Bodega.");
        }

        if (request.DestinationBranchId.HasValue)
        {
            var destinationBranch = await _branchRepository.GetByIdAsync(request.DestinationBranchId.Value)
                ?? throw new NotFoundException($"Branch {request.DestinationBranchId} not found.");

            if (destinationBranch.BranchType != BranchType.Commercial)
            {
                throw new ValidationAppException("La sucursal de destino de un movimiento de inventario debe ser de tipo Comercio.");
            }
        }
        else
        {
            _ = await _projectRepository.GetByIdAsync(request.DestinationProjectId!.Value)
                ?? throw new NotFoundException($"Project {request.DestinationProjectId} not found.");
        }

        _ = await _productRepository.GetByIdAsync(request.ProductId)
            ?? throw new NotFoundException($"Product {request.ProductId} not found.");

        var now = DateTime.UtcNow;
        var movement = new InventoryMovement
        {
            OriginBranchId = request.OriginBranchId,
            DestinationBranchId = request.DestinationBranchId,
            DestinationProjectId = request.DestinationProjectId,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            Status = InventoryMovementStatus.Sent,
            SentByUserId = sentByUserId,
            SentDate = now,
            CreatedAt = now
        };

        await _movementRepository.AddAsync(movement);

        return MapToDto(movement);
    }

    public Task<InventoryMovementDto> ValidateAsync(int id, bool approve, int actingUserId, IEnumerable<string> actingUserRoles) =>
        throw new NotImplementedException("Implemented in Task 9/10 of the Warehouse module plan.");

    public Task DeleteAsync(int id, IEnumerable<string> actingUserRoles) =>
        throw new NotImplementedException("Implemented in Task 10 of the Warehouse module plan.");

    public Task<InventoryMovementDto> GetByIdAsync(int id) =>
        throw new NotImplementedException("Implemented in Task 10 of the Warehouse module plan.");

    public Task<PagedResult<InventoryMovementDto>> GetAllByOriginBranchAsync(int branchId, int pageNumber, int pageSize) =>
        throw new NotImplementedException("Implemented in Task 10 of the Warehouse module plan.");

    private static InventoryMovementDto MapToDto(InventoryMovement movement) => new()
    {
        Id = movement.Id,
        OriginBranchId = movement.OriginBranchId,
        DestinationBranchId = movement.DestinationBranchId,
        DestinationProjectId = movement.DestinationProjectId,
        ProductId = movement.ProductId,
        Quantity = movement.Quantity,
        Status = movement.Status,
        SentByUserId = movement.SentByUserId,
        SentDate = movement.SentDate,
        ValidatedByUserId = movement.ValidatedByUserId,
        ValidatedDate = movement.ValidatedDate
    };
}
```

- [ ] **Step 5: Run the tests to verify they pass**

```bash
dotnet build
dotnet test LvTest/LvTest.csproj --filter "FullyQualifiedName~InventoryMovementServiceTests"
```

Expected: all 6 tests in this file pass (the `ValidateAsync`/`DeleteAsync` stubs are not exercised by any test yet).

- [ ] **Step 6: Mark step done**

---

## Task 9: `InventoryMovementService` — `ValidateAsync` (approve → Branch, approve → Project, deny, insufficient stock, role gate)

**Files:**
- Modify: `LvApplication/Services/Warehouse/InventoryMovementService.cs` (replace the `ValidateAsync` stub from Task 8)
- Test: `LvTest/Services/Warehouse/InventoryMovementServiceTests.cs`

**Interfaces:**
- Consumes: `IBranchInventoryRepository.GetByBranchAndProductAsync/AddAsync/UpdateAsync` (pre-existing), `IProjectInventoryItemRepository.GetByProjectAndProductAsync/AddAsync/UpdateAsync` (Task 1), `IProductRepository.GetByIdAsync` (pre-existing, needed for `Product.UnitCost`).
- Produces: `InventoryMovementService.ValidateAsync(int id, bool approve, int actingUserId, IEnumerable<string> actingUserRoles) : Task<InventoryMovementDto>` fully implemented.

- [ ] **Step 1: Write the failing tests**

Add to `LvTest/Services/Warehouse/InventoryMovementServiceTests.cs`:

```csharp
    [Fact]
    public async Task ValidateAsync_ApproveToBranch_DecrementsOriginIncrementsDestination()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "im10@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var warehouse = await CreateBranchAsync(context, user.Id, BranchType.Warehouse, "Bodega");
        var commerce = await CreateBranchAsync(context, user.Id, BranchType.Commercial, "Comercio");
        var product = await CreateValidatedProductAsync(context, user.Id, "SKU-IM-10");
        await CreateInventoryAsync(context, warehouse.Id, product.Id, 20m);
        var service = ServiceFactory.CreateInventoryMovementService(context);
        var sent = await service.CreateAsync(BuildDtoToBranch(warehouse.Id, commerce.Id, product.Id, 5), user.Id);

        var validated = await service.ValidateAsync(sent.Id, true, user.Id, new[] { "GeneralManager" });

        validated.Status.Should().Be(InventoryMovementStatus.Accepted);
        var origin = await context.BranchInventories.FirstAsync(i => i.BranchId == warehouse.Id && i.ProductId == product.Id);
        origin.Quantity.Should().Be(15m);
        var destination = await context.BranchInventories.FirstAsync(i => i.BranchId == commerce.Id && i.ProductId == product.Id);
        destination.Quantity.Should().Be(5m);
    }

    [Fact]
    public async Task ValidateAsync_ApproveToProject_IncrementsProjectInventoryWithoutTouchingExpenses()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "im11@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var warehouse = await CreateBranchAsync(context, user.Id, BranchType.Warehouse, "Bodega");
        var product = await CreateValidatedProductAsync(context, user.Id, "SKU-IM-11", unitCost: 650m);
        await CreateInventoryAsync(context, warehouse.Id, product.Id, 20m);
        var project = await CreateActiveProjectAsync(context);
        var expensesBefore = (project.CurrentDirectExpenses, project.PendingExpenses);
        var service = ServiceFactory.CreateInventoryMovementService(context);
        var sent = await service.CreateAsync(BuildDtoToProject(warehouse.Id, project.Id, product.Id, 5), user.Id);

        var validated = await service.ValidateAsync(sent.Id, true, user.Id, new[] { "GeneralManager" });

        validated.Status.Should().Be(InventoryMovementStatus.Accepted);
        var inventoryItem = await context.ProjectInventoryItems.FirstAsync(i => i.ProjectId == project.Id && i.ProductId == product.Id);
        inventoryItem.CurrentQuantity.Should().Be(5m);
        inventoryItem.ReferenceUnitCost.Should().Be(650m);
        inventoryItem.MaterialId.Should().BeNull();
        var projectAfter = await context.Projects.FindAsync(project.Id);
        projectAfter!.CurrentDirectExpenses.Should().Be(expensesBefore.CurrentDirectExpenses, "the material's cost was already recognized when it entered the warehouse via ProductIncorporationTicket");
        projectAfter.PendingExpenses.Should().Be(expensesBefore.PendingExpenses);
    }

    [Fact]
    public async Task ValidateAsync_Deny_NoInventoryChanges()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "im12@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var warehouse = await CreateBranchAsync(context, user.Id, BranchType.Warehouse, "Bodega");
        var commerce = await CreateBranchAsync(context, user.Id, BranchType.Commercial, "Comercio");
        var product = await CreateValidatedProductAsync(context, user.Id, "SKU-IM-12");
        await CreateInventoryAsync(context, warehouse.Id, product.Id, 20m);
        var service = ServiceFactory.CreateInventoryMovementService(context);
        var sent = await service.CreateAsync(BuildDtoToBranch(warehouse.Id, commerce.Id, product.Id, 5), user.Id);

        var denied = await service.ValidateAsync(sent.Id, false, user.Id, new[] { "GeneralManager" });

        denied.Status.Should().Be(InventoryMovementStatus.Denied);
        var origin = await context.BranchInventories.FirstAsync(i => i.BranchId == warehouse.Id && i.ProductId == product.Id);
        origin.Quantity.Should().Be(20m);
        (await context.BranchInventories.AnyAsync(i => i.BranchId == commerce.Id && i.ProductId == product.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_ApproveInsufficientOriginStock_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "im13@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var warehouse = await CreateBranchAsync(context, user.Id, BranchType.Warehouse, "Bodega");
        var commerce = await CreateBranchAsync(context, user.Id, BranchType.Commercial, "Comercio");
        var product = await CreateValidatedProductAsync(context, user.Id, "SKU-IM-13");
        await CreateInventoryAsync(context, warehouse.Id, product.Id, 3m);
        var service = ServiceFactory.CreateInventoryMovementService(context);
        var sent = await service.CreateAsync(BuildDtoToBranch(warehouse.Id, commerce.Id, product.Id, 5), user.Id);

        var act = () => service.ValidateAsync(sent.Id, true, user.Id, new[] { "GeneralManager" });

        await act.Should().ThrowAsync<ValidationAppException>();
        var origin = await context.BranchInventories.FirstAsync(i => i.BranchId == warehouse.Id && i.ProductId == product.Id);
        origin.Quantity.Should().Be(3m, "a failed validation must not decrement stock");
    }

    [Fact]
    public async Task ValidateAsync_ByBranchAdmin_ThrowsForbiddenException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "im14dir@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var branchAdmin = await TestUserFactory.CreateAsync(context, "im14ba@example.com", roleId: TestUserFactory.BranchAdminRoleId);
        var warehouse = await CreateBranchAsync(context, director.Id, BranchType.Warehouse, "Bodega");
        var commerce = await CreateBranchAsync(context, director.Id, BranchType.Commercial, "Comercio");
        var product = await CreateValidatedProductAsync(context, director.Id, "SKU-IM-14");
        await CreateInventoryAsync(context, warehouse.Id, product.Id, 20m);
        var service = ServiceFactory.CreateInventoryMovementService(context);
        var sent = await service.CreateAsync(BuildDtoToBranch(warehouse.Id, commerce.Id, product.Id, 5), director.Id);

        var act = () => service.ValidateAsync(sent.Id, true, branchAdmin.Id, new[] { "BranchAdmin" });

        await act.Should().ThrowAsync<ForbiddenException>();
    }
```

- [ ] **Step 2: Run the tests to verify they fail**

```bash
dotnet test LvTest/LvTest.csproj --filter "FullyQualifiedName~InventoryMovementServiceTests"
```

Expected: the 5 new tests throw `NotImplementedException` (from the Task 8 stub) instead of the expected results/exceptions — correct "fails for the right reason."

- [ ] **Step 3: Implement `ValidateAsync`**

In `LvApplication/Services/Warehouse/InventoryMovementService.cs`, add these two private constants right after the class opening brace (before the fields):

```csharp
    private const string GeneralManagerRole = "GeneralManager";
    private const string OperationsDirectorRole = "OperationsDirector";

    private static bool CanValidate(IEnumerable<string> actingUserRoles) =>
        actingUserRoles.Contains(GeneralManagerRole) || actingUserRoles.Contains(OperationsDirectorRole);
```

Replace the `ValidateAsync` stub with:

```csharp
    public async Task<InventoryMovementDto> ValidateAsync(int id, bool approve, int actingUserId, IEnumerable<string> actingUserRoles)
    {
        if (!CanValidate(actingUserRoles))
        {
            throw new ForbiddenException("Solo Gerente General o Director de Operaciones pueden validar movimientos de inventario.");
        }

        var movement = await _movementRepository.GetByIdAsync(id) ?? throw new NotFoundException($"InventoryMovement {id} not found.");

        if (movement.Status != InventoryMovementStatus.Sent)
        {
            throw new ValidationAppException("Solo se puede validar un movimiento en estado Sent.");
        }

        if (approve)
        {
            var originInventory = await _branchInventoryRepository.GetByBranchAndProductAsync(movement.OriginBranchId, movement.ProductId);
            if (originInventory is null || originInventory.Quantity < movement.Quantity)
            {
                throw new ValidationAppException("Stock insuficiente en la bodega de origen para completar el traslado.");
            }

            originInventory.Quantity -= movement.Quantity;
            originInventory.UpdatedAt = DateTime.UtcNow;
            await _branchInventoryRepository.UpdateAsync(originInventory);

            if (movement.DestinationBranchId.HasValue)
            {
                await IncrementBranchInventoryAsync(movement.DestinationBranchId.Value, movement.ProductId, movement.Quantity);
            }
            else
            {
                var product = await _productRepository.GetByIdAsync(movement.ProductId)
                    ?? throw new NotFoundException($"Product {movement.ProductId} not found.");
                await IncrementProjectInventoryAsync(movement.DestinationProjectId!.Value, movement.ProductId, movement.Quantity, product.UnitCost);
            }

            movement.Status = InventoryMovementStatus.Accepted;
        }
        else
        {
            movement.Status = InventoryMovementStatus.Denied;
        }

        movement.ValidatedByUserId = actingUserId;
        movement.ValidatedDate = DateTime.UtcNow;
        movement.UpdatedAt = DateTime.UtcNow;

        await _movementRepository.UpdateAsync(movement);

        return MapToDto(movement);
    }

    private async Task IncrementBranchInventoryAsync(int branchId, int productId, decimal quantity)
    {
        var inventory = await _branchInventoryRepository.GetByBranchAndProductAsync(branchId, productId);

        if (inventory is null)
        {
            inventory = new LvDomain.Entities.Commercial.BranchInventory
            {
                BranchId = branchId,
                ProductId = productId,
                Quantity = quantity,
                MinimumStock = 0,
                CreatedAt = DateTime.UtcNow
            };
            await _branchInventoryRepository.AddAsync(inventory);
        }
        else
        {
            inventory.Quantity += quantity;
            inventory.UpdatedAt = DateTime.UtcNow;
            await _branchInventoryRepository.UpdateAsync(inventory);
        }
    }

    private async Task IncrementProjectInventoryAsync(int projectId, int productId, decimal quantity, decimal unitCost)
    {
        var item = await _projectInventoryItemRepository.GetByProjectAndProductAsync(projectId, productId);

        if (item is null)
        {
            item = new LvDomain.Entities.Inventory.ProjectInventoryItem
            {
                ProjectId = projectId,
                ProductId = productId,
                CurrentQuantity = quantity,
                ReferenceUnitCost = unitCost,
                CreatedAt = DateTime.UtcNow
            };
            await _projectInventoryItemRepository.AddAsync(item);
        }
        else
        {
            item.CurrentQuantity += quantity;
            item.ReferenceUnitCost = unitCost;
            item.UpdatedAt = DateTime.UtcNow;
            await _projectInventoryItemRepository.UpdateAsync(item);
        }
    }
```

(Fully-qualified type names `LvDomain.Entities.Commercial.BranchInventory` and `LvDomain.Entities.Inventory.ProjectInventoryItem` are used inline here to avoid an ambiguous-reference risk — the file already has `using LvApplication.Services.Commercial;` for `IBranchInventoryRepository`, and `LvDomain.Entities.Commercial` isn't otherwise imported. If preferred, add `using LvDomain.Entities.Commercial;` and `using LvDomain.Entities.Inventory;` to the top instead and drop the qualification — either compiles fine, no naming collisions exist in this codebase.)

- [ ] **Step 4: Run the tests to verify they pass**

```bash
dotnet build
dotnet test LvTest/LvTest.csproj --filter "FullyQualifiedName~InventoryMovementServiceTests"
```

Expected: all 11 tests in the file pass (6 from Task 8 + 5 new).

- [ ] **Step 5: Mark step done**

---

## Task 10: `InventoryMovementService` — `DeleteAsync`, `GetByIdAsync`, `GetAllByOriginBranchAsync` + Delete role gate

**Files:**
- Modify: `LvApplication/Services/Warehouse/InventoryMovementService.cs` (replace the three remaining stubs from Task 8)
- Test: `LvTest/Services/Warehouse/InventoryMovementServiceTests.cs`

**Interfaces:**
- Produces: `InventoryMovementService.DeleteAsync(int, IEnumerable<string>)`, `.GetByIdAsync(int)`, `.GetAllByOriginBranchAsync(int,int,int)` fully implemented — consumed by Task 11 (controller).

- [ ] **Step 1: Write the failing tests**

Add to `LvTest/Services/Warehouse/InventoryMovementServiceTests.cs`:

```csharp
    [Fact]
    public async Task DeleteAsync_SentMovement_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "im20@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var warehouse = await CreateBranchAsync(context, user.Id, BranchType.Warehouse, "Bodega");
        var commerce = await CreateBranchAsync(context, user.Id, BranchType.Commercial, "Comercio");
        var product = await CreateValidatedProductAsync(context, user.Id, "SKU-IM-20");
        await CreateInventoryAsync(context, warehouse.Id, product.Id, 20m);
        var service = ServiceFactory.CreateInventoryMovementService(context);
        var sent = await service.CreateAsync(BuildDtoToBranch(warehouse.Id, commerce.Id, product.Id, 5), user.Id);

        await service.DeleteAsync(sent.Id, new[] { "GeneralManager" });

        (await context.InventoryMovements.FindAsync(sent.Id)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_AcceptedMovement_ThrowsValidationAppException()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "im21@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var warehouse = await CreateBranchAsync(context, user.Id, BranchType.Warehouse, "Bodega");
        var commerce = await CreateBranchAsync(context, user.Id, BranchType.Commercial, "Comercio");
        var product = await CreateValidatedProductAsync(context, user.Id, "SKU-IM-21");
        await CreateInventoryAsync(context, warehouse.Id, product.Id, 20m);
        var service = ServiceFactory.CreateInventoryMovementService(context);
        var sent = await service.CreateAsync(BuildDtoToBranch(warehouse.Id, commerce.Id, product.Id, 5), user.Id);
        await service.ValidateAsync(sent.Id, true, user.Id, new[] { "GeneralManager" });

        var act = () => service.DeleteAsync(sent.Id, new[] { "GeneralManager" });

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task DeleteAsync_ByBranchAdmin_ThrowsForbiddenException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "im22dir@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var branchAdmin = await TestUserFactory.CreateAsync(context, "im22ba@example.com", roleId: TestUserFactory.BranchAdminRoleId);
        var warehouse = await CreateBranchAsync(context, director.Id, BranchType.Warehouse, "Bodega");
        var commerce = await CreateBranchAsync(context, director.Id, BranchType.Commercial, "Comercio");
        var product = await CreateValidatedProductAsync(context, director.Id, "SKU-IM-22");
        await CreateInventoryAsync(context, warehouse.Id, product.Id, 20m);
        var service = ServiceFactory.CreateInventoryMovementService(context);
        var sent = await service.CreateAsync(BuildDtoToBranch(warehouse.Id, commerce.Id, product.Id, 5), director.Id);

        var act = () => service.DeleteAsync(sent.Id, new[] { "BranchAdmin" });

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsMovement()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(context, "im23@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var warehouse = await CreateBranchAsync(context, user.Id, BranchType.Warehouse, "Bodega");
        var commerce = await CreateBranchAsync(context, user.Id, BranchType.Commercial, "Comercio");
        var product = await CreateValidatedProductAsync(context, user.Id, "SKU-IM-23");
        await CreateInventoryAsync(context, warehouse.Id, product.Id, 20m);
        var service = ServiceFactory.CreateInventoryMovementService(context);
        var sent = await service.CreateAsync(BuildDtoToBranch(warehouse.Id, commerce.Id, product.Id, 5), user.Id);

        var result = await service.GetByIdAsync(sent.Id);

        result.Id.Should().Be(sent.Id);
    }
```

- [ ] **Step 2: Run the tests to verify they fail**

```bash
dotnet test LvTest/LvTest.csproj --filter "FullyQualifiedName~InventoryMovementServiceTests"
```

Expected: the 4 new tests throw `NotImplementedException` from the Task 8 stubs.

- [ ] **Step 3: Implement the remaining three methods**

In `LvApplication/Services/Warehouse/InventoryMovementService.cs`, replace the three remaining stubs with:

```csharp
    public async Task DeleteAsync(int id, IEnumerable<string> actingUserRoles)
    {
        if (!CanValidate(actingUserRoles))
        {
            throw new ForbiddenException("Solo Gerente General o Director de Operaciones pueden eliminar movimientos de inventario.");
        }

        var movement = await _movementRepository.GetByIdAsync(id) ?? throw new NotFoundException($"InventoryMovement {id} not found.");

        if (movement.Status != InventoryMovementStatus.Sent)
        {
            throw new ValidationAppException("Solo se puede eliminar un movimiento en estado Sent.");
        }

        await _movementRepository.DeleteAsync(movement);
    }

    public async Task<InventoryMovementDto> GetByIdAsync(int id)
    {
        var movement = await _movementRepository.GetByIdAsync(id) ?? throw new NotFoundException($"InventoryMovement {id} not found.");
        return MapToDto(movement);
    }

    public async Task<PagedResult<InventoryMovementDto>> GetAllByOriginBranchAsync(int branchId, int pageNumber, int pageSize)
    {
        var (items, totalCount) = await _movementRepository.GetPagedByOriginBranchAsync(branchId, pageNumber, pageSize);

        return new PagedResult<InventoryMovementDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
```

- [ ] **Step 4: Run the tests to verify they pass**

```bash
dotnet build
dotnet test LvTest/LvTest.csproj --filter "FullyQualifiedName~InventoryMovementServiceTests"
```

Expected: all 15 tests in the file pass (11 from Tasks 8-9 + 4 new).

- [ ] **Step 5: Mark step done**

---

## Task 11: DI registration + `ServiceFactory` finalization

**Files:**
- Modify: `LvApi/Extensions/ServiceCollectionExtensions.cs`

Note: `LvTest/Common/ServiceFactory.cs` was already updated in Task 8 Step 2 — this task only touches production DI, which is separate from the test factory.

- [ ] **Step 1: Register the repository and service**

`LvApi/Extensions/ServiceCollectionExtensions.cs` — add `using LvApplication.Services.Warehouse;` and `using LvInfrastructure.Repositories.Warehouse;` to the usings.

In `AddInfrastructureServices`, add right after the existing Commercial block (`services.AddScoped<IInvoiceRepository, InvoiceRepository>();`):

```csharp
        services.AddScoped<IInventoryMovementRepository, InventoryMovementRepository>();
```

In `AddApplicationServices`, add right after the existing Commercial block (`services.AddScoped<IInvoiceService, InvoiceService>();`):

```csharp
        services.AddScoped<IInventoryMovementService, InventoryMovementService>();
```

- [ ] **Step 2: Build to confirm it compiles**

```bash
dotnet build
```

Expected: 0 errors. (`AddValidatorsFromAssembly` already auto-registers `CreateInventoryMovementDtoValidator`, `CreateWorkerDtoValidator`, `UpdateWorkerDtoValidator` — no manual validator registration needed for any task in this plan.)

- [ ] **Step 3: Mark step done**

---

## Task 12: `InventoryMovementsController`

**Files:**
- Create: `LvApi/Controllers/Warehouse/InventoryMovementsController.cs`

**Interfaces:**
- Consumes: `IInventoryMovementService` (Tasks 8-10).

- [ ] **Step 1: Create the controller**

`LvApi/Controllers/Warehouse/InventoryMovementsController.cs`:

```csharp
using System.Security.Claims;
using LvApplication.Common;
using LvApplication.DTOs.Warehouse;
using LvApplication.Services.Warehouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.Warehouse;

[ApiController]
[Route("api/inventory-movements")]
[Authorize]
public class InventoryMovementsController : ControllerBase
{
    private readonly IInventoryMovementService _movementService;

    public InventoryMovementsController(IInventoryMovementService movementService)
    {
        _movementService = movementService;
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector,BranchAdmin,BusinessManager")]
    [HttpPost]
    public async Task<ActionResult<InventoryMovementDto>> Create(CreateInventoryMovementDto request)
    {
        var result = await _movementService.CreateAsync(request, GetCurrentUserId());
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpPost("{id:int}/validate")]
    public async Task<ActionResult<InventoryMovementDto>> Validate(int id, ValidateInventoryMovementDto request)
    {
        var result = await _movementService.ValidateAsync(id, request.Approve, GetCurrentUserId(), GetCurrentRoles());
        return Ok(result);
    }

    [Authorize(Roles = "GeneralManager,OperationsDirector")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _movementService.DeleteAsync(id, GetCurrentRoles());
        return NoContent();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InventoryMovementDto>> GetById(int id)
    {
        var result = await _movementService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpGet("~/api/branches/{branchId:int}/inventory-movements")]
    public async Task<ActionResult<PagedResult<InventoryMovementDto>>> GetAllByOriginBranch(
        int branchId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _movementService.GetAllByOriginBranchAsync(branchId, pageNumber, pageSize);
        return Ok(result);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        return int.Parse(userIdClaim!.Value);
    }

    private List<string> GetCurrentRoles() =>
        User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
}
```

- [ ] **Step 2: Build to confirm it compiles**

```bash
dotnet build
```

- [ ] **Step 3: Mark step done** (no controller-level tests — this codebase's test suite is service-layer only, per every prior phase's convention)

---

## Task 13: Final full-solution verification

- [ ] **Step 1: Full clean build**

```bash
dotnet build
```

Expected: `Build succeeded`, 0 errors.

- [ ] **Step 2: Full test suite**

```bash
dotnet test LvTest/LvTest.csproj
```

Expected: 100% pass, pristine output (no new warnings beyond the pre-existing `CA1861`/`CA1305` noise already present before this phase).

- [ ] **Step 3: Report to the user**

Report: final X/X test count, confirmation no migration was generated, and the design decisions/ambiguities list (schema bridge for `ProjectInventoryItem`, Worker→Branch built from scratch, `DeleteAsync` deviating from the Fase-12 precedent to take `actingUserRoles`, no `Product.Status == Validated` gate on `InventoryMovement.CreateAsync` since the spec didn't request it, overwrite-not-weighted-average cost behavior matching `MaterialTicketService.ApplyAsync`).

- [ ] **Step 4: Mark step done** (no commit — not requested this session)
