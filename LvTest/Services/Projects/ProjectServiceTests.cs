using FluentAssertions;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Projects;
using LvDomain.Entities.Branches;
using LvDomain.Entities.Budgets;
using LvDomain.Entities.Customers;
using LvDomain.Entities.Offers;
using LvDomain.Entities.Workers;
using LvDomain.Enums;
using LvTest.Common;
using Microsoft.EntityFrameworkCore;

namespace LvTest.Services.Projects;

public class ProjectServiceTests
{
    private static async Task<Customer> CreateProjectCustomerAsync(
        LvInfrastructure.Persistence.AppDbContext context
    )
    {
        var customer = new Customer
        {
            Name = "Project Customer",
            CustomerType = CustomerType.Project,
            Status = ActiveStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer;
    }

    private static async Task<Branch> CreateBranchAsync(
        LvInfrastructure.Persistence.AppDbContext context,
        int operationsDirectorId,
        BranchType branchType = BranchType.Office
    )
    {
        var branch = new Branch
        {
            Name = "Test Branch",
            City = "San Jose",
            Province = "San Jose",
            Status = BranchStatus.Active,
            BranchType = branchType,
            OperationsDirectorId = operationsDirectorId,
            CreatedAt = DateTime.UtcNow,
        };
        context.Branches.Add(branch);
        await context.SaveChangesAsync();
        return branch;
    }

    private static async Task<Budget> CreateBudgetAsync(
        LvInfrastructure.Persistence.AppDbContext context,
        int customerId,
        int branchId,
        int createdByUserId,
        BudgetStatus status
    )
    {
        var budget = new Budget
        {
            CustomerId = customerId,
            BranchId = branchId,
            Name = "Edificio Test",
            Status = status,
            UtilityPercentage = 10,
            IndirectCostsTotal = 50,
            TotalBudget = 1000,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
        };
        context.Budgets.Add(budget);
        await context.SaveChangesAsync();
        return budget;
    }

    private static async Task<Offer> CreateOfferAsync(
        LvInfrastructure.Persistence.AppDbContext context,
        int budgetId,
        int customerId,
        int createdByUserId,
        OfferStatus status
    )
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
            Status = status,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
        };
        context.Offers.Add(offer);
        await context.SaveChangesAsync();
        return offer;
    }

    private static async Task<Worker> CreateWorkerAsync(
        LvInfrastructure.Persistence.AppDbContext context,
        ActiveStatus status = ActiveStatus.Active
    )
    {
        var worker = new Worker
        {
            Name = "Test Worker",
            Status = status,
            Category = WorkerCategory.Construction,
            Type = WorkerType.Laborer,
            HourlyRate = 6,
            CreatedAt = DateTime.UtcNow,
        };
        context.Workers.Add(worker);
        await context.SaveChangesAsync();
        return worker;
    }

    private static CreateProjectDto BuildCreateDto(int offerId, int branchId) =>
        new()
        {
            OfferId = offerId,
            BranchId = branchId,
            StartDate = new DateTime(2026, 3, 1),
        };

    [Fact]
    public async Task CreateProjectAsync_ValidOffer_CreatesProjectSuccessfully()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-project-create@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var creator = await TestUserFactory.CreateAsync(
            context,
            "gm-project-create@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateBudgetAsync(
            context,
            customer.Id,
            branch.Id,
            creator.Id,
            BudgetStatus.ClientApproved
        );
        var offer = await CreateOfferAsync(
            context,
            budget.Id,
            customer.Id,
            creator.Id,
            OfferStatus.ClientAccepted
        );
        var service = ServiceFactory.CreateProjectService(context);

        var result = await service.CreateProjectAsync(
            BuildCreateDto(offer.Id, branch.Id),
            creator.Id
        );

        result.Id.Should().BeGreaterThan(0);
        result.OfferId.Should().Be(offer.Id);
        result.BudgetId.Should().Be(budget.Id);
        result.CustomerId.Should().Be(customer.Id);
        result.BranchId.Should().Be(branch.Id);
        result.ProjectType.Should().Be(ProjectType.TurnKey);
        result.Status.Should().Be(ProjectStatus.Active);
        result.StartDate.Should().Be(new DateTime(2026, 3, 1));
        result.EndDate.Should().Be(offer.EstimatedDeliveryDate);
        result.WeeksCounter.Should().Be(0);
        result.WorkersUsedCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateProjectAsync_OfferNotClientAccepted_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-project-offernotaccepted@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var creator = await TestUserFactory.CreateAsync(
            context,
            "gm-project-offernotaccepted@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateBudgetAsync(
            context,
            customer.Id,
            branch.Id,
            creator.Id,
            BudgetStatus.ClientApproved
        );
        var offer = await CreateOfferAsync(
            context,
            budget.Id,
            customer.Id,
            creator.Id,
            OfferStatus.SentToClient
        );
        var service = ServiceFactory.CreateProjectService(context);

        var act = async () =>
            await service.CreateProjectAsync(BuildCreateDto(offer.Id, branch.Id), creator.Id);

        var exception = await act.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Message.Should().Contain("Aceptada por el Cliente");
    }

    [Fact]
    public async Task CreateProjectAsync_BudgetNotClientApproved_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-project-budgetnotapproved@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var creator = await TestUserFactory.CreateAsync(
            context,
            "gm-project-budgetnotapproved@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        // Data-integrity edge case: Offer says ClientAccepted but its Budget was never actually approved.
        var budget = await CreateBudgetAsync(
            context,
            customer.Id,
            branch.Id,
            creator.Id,
            BudgetStatus.Sent
        );
        var offer = await CreateOfferAsync(
            context,
            budget.Id,
            customer.Id,
            creator.Id,
            OfferStatus.ClientAccepted
        );
        var service = ServiceFactory.CreateProjectService(context);

        var act = async () =>
            await service.CreateProjectAsync(BuildCreateDto(offer.Id, branch.Id), creator.Id);

        var exception = await act.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Message.Should().Contain("Aprobado por el Cliente");
    }

    [Fact]
    public async Task CreateProjectAsync_OfferAlreadyHasProject_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-project-dup@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var creator = await TestUserFactory.CreateAsync(
            context,
            "gm-project-dup@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateBudgetAsync(
            context,
            customer.Id,
            branch.Id,
            creator.Id,
            BudgetStatus.ClientApproved
        );
        var offer = await CreateOfferAsync(
            context,
            budget.Id,
            customer.Id,
            creator.Id,
            OfferStatus.ClientAccepted
        );
        var service = ServiceFactory.CreateProjectService(context);

        await service.CreateProjectAsync(BuildCreateDto(offer.Id, branch.Id), creator.Id);

        var act = async () =>
            await service.CreateProjectAsync(BuildCreateDto(offer.Id, branch.Id), creator.Id);

        var exception = await act.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Message.Should().Contain("ya tiene un proyecto");
    }

    [Fact]
    public async Task CreateProjectAsync_BranchNotOfficeType_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-project-notoffice@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var creator = await TestUserFactory.CreateAsync(
            context,
            "gm-project-notoffice@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var customer = await CreateProjectCustomerAsync(context);
        var officeBranch = await CreateBranchAsync(context, director.Id);
        var warehouseBranch = await CreateBranchAsync(context, director.Id, BranchType.Warehouse);
        var budget = await CreateBudgetAsync(
            context,
            customer.Id,
            officeBranch.Id,
            creator.Id,
            BudgetStatus.ClientApproved
        );
        var offer = await CreateOfferAsync(
            context,
            budget.Id,
            customer.Id,
            creator.Id,
            OfferStatus.ClientAccepted
        );
        var service = ServiceFactory.CreateProjectService(context);

        var act = async () =>
            await service.CreateProjectAsync(
                BuildCreateDto(offer.Id, warehouseBranch.Id),
                creator.Id
            );

        var exception = await act.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Message.Should().Contain("tipo Oficina");
    }

    [Fact]
    public async Task UpdateEndDateAsync_CreatesHistoryRecordAndUpdatesDate()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-project-enddate@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var creator = await TestUserFactory.CreateAsync(
            context,
            "gm-project-enddate@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateBudgetAsync(
            context,
            customer.Id,
            branch.Id,
            creator.Id,
            BudgetStatus.ClientApproved
        );
        var offer = await CreateOfferAsync(
            context,
            budget.Id,
            customer.Id,
            creator.Id,
            OfferStatus.ClientAccepted
        );
        var service = ServiceFactory.CreateProjectService(context);

        var created = await service.CreateProjectAsync(
            BuildCreateDto(offer.Id, branch.Id),
            creator.Id
        );
        var previousEndDate = created.EndDate;
        var newEndDate = previousEndDate.AddDays(14);

        var updated = await service.UpdateEndDateAsync(
            created.Id,
            new UpdateEndDateDto { NewEndDate = newEndDate, Reason = "Retraso por lluvias" },
            creator.Id
        );

        updated.EndDate.Should().Be(newEndDate);

        var history = await service.GetEndDateHistoryAsync(created.Id);
        history.Should().ContainSingle();
        history[0].PreviousDate.Should().Be(previousEndDate);
        history[0].NewDate.Should().Be(newEndDate);
        history[0].Reason.Should().Be("Retraso por lluvias");
        history[0].UserId.Should().Be(creator.Id);
    }

    [Fact]
    public async Task UpdateEndDateAsync_WithoutReason_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-project-enddatenoreason@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var creator = await TestUserFactory.CreateAsync(
            context,
            "gm-project-enddatenoreason@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateBudgetAsync(
            context,
            customer.Id,
            branch.Id,
            creator.Id,
            BudgetStatus.ClientApproved
        );
        var offer = await CreateOfferAsync(
            context,
            budget.Id,
            customer.Id,
            creator.Id,
            OfferStatus.ClientAccepted
        );
        var service = ServiceFactory.CreateProjectService(context);

        var created = await service.CreateProjectAsync(
            BuildCreateDto(offer.Id, branch.Id),
            creator.Id
        );

        var act = async () =>
            await service.UpdateEndDateAsync(
                created.Id,
                new UpdateEndDateDto { NewEndDate = created.EndDate.AddDays(7), Reason = "" },
                creator.Id
            );

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task AssignWorkerAsync_NewWorker_CreatesAssignment()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-project-assign@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var creator = await TestUserFactory.CreateAsync(
            context,
            "gm-project-assign@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateBudgetAsync(
            context,
            customer.Id,
            branch.Id,
            creator.Id,
            BudgetStatus.ClientApproved
        );
        var offer = await CreateOfferAsync(
            context,
            budget.Id,
            customer.Id,
            creator.Id,
            OfferStatus.ClientAccepted
        );
        var worker = await CreateWorkerAsync(context);
        var service = ServiceFactory.CreateProjectService(context);

        var created = await service.CreateProjectAsync(
            BuildCreateDto(offer.Id, branch.Id),
            creator.Id
        );

        var result = await service.AssignWorkerAsync(
            created.Id,
            new AssignWorkerDto { WorkerId = worker.Id },
            creator.Id
        );

        result.WorkersUsedCount.Should().Be(1);
        result
            .Workers.Should()
            .ContainSingle(w =>
                w.WorkerId == worker.Id && w.IsActive && w.AssignedByUserId == creator.Id
            );
    }

    [Fact]
    public async Task AssignWorkerAsync_ReactivatesInactiveAssignment_DoesNotDuplicate()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-project-reassign@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var creator = await TestUserFactory.CreateAsync(
            context,
            "gm-project-reassign@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateBudgetAsync(
            context,
            customer.Id,
            branch.Id,
            creator.Id,
            BudgetStatus.ClientApproved
        );
        var offer = await CreateOfferAsync(
            context,
            budget.Id,
            customer.Id,
            creator.Id,
            OfferStatus.ClientAccepted
        );
        var worker = await CreateWorkerAsync(context);
        var service = ServiceFactory.CreateProjectService(context);

        var created = await service.CreateProjectAsync(
            BuildCreateDto(offer.Id, branch.Id),
            creator.Id
        );
        await service.AssignWorkerAsync(
            created.Id,
            new AssignWorkerDto { WorkerId = worker.Id },
            creator.Id
        );
        await service.UnassignWorkerAsync(created.Id, worker.Id);

        var result = await service.AssignWorkerAsync(
            created.Id,
            new AssignWorkerDto { WorkerId = worker.Id },
            creator.Id
        );

        result.WorkersUsedCount.Should().Be(1);
        result.Workers.Should().ContainSingle(w => w.WorkerId == worker.Id && w.IsActive);

        var stored = await context
            .ProjectWorkers.Where(pw => pw.ProjectId == created.Id && pw.WorkerId == worker.Id)
            .ToListAsync();
        stored.Should().HaveCount(1);
    }

    [Fact]
    public async Task AssignWorkerAsync_InactiveWorker_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-project-inactiveworker@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var creator = await TestUserFactory.CreateAsync(
            context,
            "gm-project-inactiveworker@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateBudgetAsync(
            context,
            customer.Id,
            branch.Id,
            creator.Id,
            BudgetStatus.ClientApproved
        );
        var offer = await CreateOfferAsync(
            context,
            budget.Id,
            customer.Id,
            creator.Id,
            OfferStatus.ClientAccepted
        );
        var worker = await CreateWorkerAsync(context, ActiveStatus.Inactive);
        var service = ServiceFactory.CreateProjectService(context);

        var created = await service.CreateProjectAsync(
            BuildCreateDto(offer.Id, branch.Id),
            creator.Id
        );

        var act = async () =>
            await service.AssignWorkerAsync(
                created.Id,
                new AssignWorkerDto { WorkerId = worker.Id },
                creator.Id
            );

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task UnassignWorkerAsync_SoftDeactivatesWithoutRemovingRecord()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-project-unassign@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var creator = await TestUserFactory.CreateAsync(
            context,
            "gm-project-unassign@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateBudgetAsync(
            context,
            customer.Id,
            branch.Id,
            creator.Id,
            BudgetStatus.ClientApproved
        );
        var offer = await CreateOfferAsync(
            context,
            budget.Id,
            customer.Id,
            creator.Id,
            OfferStatus.ClientAccepted
        );
        var worker = await CreateWorkerAsync(context);
        var service = ServiceFactory.CreateProjectService(context);

        var created = await service.CreateProjectAsync(
            BuildCreateDto(offer.Id, branch.Id),
            creator.Id
        );
        await service.AssignWorkerAsync(
            created.Id,
            new AssignWorkerDto { WorkerId = worker.Id },
            creator.Id
        );

        var result = await service.UnassignWorkerAsync(created.Id, worker.Id);

        result.WorkersUsedCount.Should().Be(0);
        result.Workers.Should().BeEmpty();

        var stored = await context.ProjectWorkers.FirstOrDefaultAsync(pw =>
            pw.ProjectId == created.Id && pw.WorkerId == worker.Id
        );
        stored.Should().NotBeNull();
        stored!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DecrementWeekCounterAsync_WithoutGeneralManagerRole_ThrowsForbiddenException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-project-decrement-badrole@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var creator = await TestUserFactory.CreateAsync(
            context,
            "gm-project-decrement-badrole@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateBudgetAsync(
            context,
            customer.Id,
            branch.Id,
            creator.Id,
            BudgetStatus.ClientApproved
        );
        var offer = await CreateOfferAsync(
            context,
            budget.Id,
            customer.Id,
            creator.Id,
            OfferStatus.ClientAccepted
        );
        var service = ServiceFactory.CreateProjectService(context);

        var created = await service.CreateProjectAsync(
            BuildCreateDto(offer.Id, branch.Id),
            creator.Id
        );
        await service.IncrementWeekCounterAsync(created.Id);

        var act = async () =>
            await service.DecrementWeekCounterAsync(created.Id, TestRoles.OperationsDirector);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task DecrementWeekCounterAsync_AsGeneralManager_DecrementsCounter()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-project-decrement-ok@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var creator = await TestUserFactory.CreateAsync(
            context,
            "gm-project-decrement-ok@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateBudgetAsync(
            context,
            customer.Id,
            branch.Id,
            creator.Id,
            BudgetStatus.ClientApproved
        );
        var offer = await CreateOfferAsync(
            context,
            budget.Id,
            customer.Id,
            creator.Id,
            OfferStatus.ClientAccepted
        );
        var service = ServiceFactory.CreateProjectService(context);

        var created = await service.CreateProjectAsync(
            BuildCreateDto(offer.Id, branch.Id),
            creator.Id
        );
        await service.IncrementWeekCounterAsync(created.Id);
        await service.IncrementWeekCounterAsync(created.Id);

        await service.DecrementWeekCounterAsync(created.Id, TestRoles.GeneralManager);

        var result = await service.GetByIdAsync(created.Id);
        result.WeeksCounter.Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_FreshProjectWithoutActivity_DeletesSuccessfully()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-project-delete-clean@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var creator = await TestUserFactory.CreateAsync(
            context,
            "gm-project-delete-clean@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateBudgetAsync(
            context,
            customer.Id,
            branch.Id,
            creator.Id,
            BudgetStatus.ClientApproved
        );
        var offer = await CreateOfferAsync(
            context,
            budget.Id,
            customer.Id,
            creator.Id,
            OfferStatus.ClientAccepted
        );
        var service = ServiceFactory.CreateProjectService(context);

        var created = await service.CreateProjectAsync(
            BuildCreateDto(offer.Id, branch.Id),
            creator.Id
        );

        await service.DeleteAsync(created.Id);

        var stored = await context.Projects.FindAsync(created.Id);
        stored.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithActiveWorker_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-project-delete-activeworker@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var creator = await TestUserFactory.CreateAsync(
            context,
            "gm-project-delete-activeworker@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateBudgetAsync(
            context,
            customer.Id,
            branch.Id,
            creator.Id,
            BudgetStatus.ClientApproved
        );
        var offer = await CreateOfferAsync(
            context,
            budget.Id,
            customer.Id,
            creator.Id,
            OfferStatus.ClientAccepted
        );
        var worker = await CreateWorkerAsync(context);
        var service = ServiceFactory.CreateProjectService(context);

        var created = await service.CreateProjectAsync(
            BuildCreateDto(offer.Id, branch.Id),
            creator.Id
        );
        await service.AssignWorkerAsync(
            created.Id,
            new AssignWorkerDto { WorkerId = worker.Id },
            creator.Id
        );

        var act = async () => await service.DeleteAsync(created.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task DeleteAsync_WithInactiveWorkerRecord_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-project-delete-inactiveworker@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var creator = await TestUserFactory.CreateAsync(
            context,
            "gm-project-delete-inactiveworker@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateBudgetAsync(
            context,
            customer.Id,
            branch.Id,
            creator.Id,
            BudgetStatus.ClientApproved
        );
        var offer = await CreateOfferAsync(
            context,
            budget.Id,
            customer.Id,
            creator.Id,
            OfferStatus.ClientAccepted
        );
        var worker = await CreateWorkerAsync(context);
        var service = ServiceFactory.CreateProjectService(context);

        var created = await service.CreateProjectAsync(
            BuildCreateDto(offer.Id, branch.Id),
            creator.Id
        );
        await service.AssignWorkerAsync(
            created.Id,
            new AssignWorkerDto { WorkerId = worker.Id },
            creator.Id
        );
        // Unassigning only sets IsActive = false; the row still exists, so it must still block deletion.
        await service.UnassignWorkerAsync(created.Id, worker.Id);

        var act = async () => await service.DeleteAsync(created.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task DeleteAsync_WithWeeksCounterGreaterThanZero_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-project-delete-weeks@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var creator = await TestUserFactory.CreateAsync(
            context,
            "gm-project-delete-weeks@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateBudgetAsync(
            context,
            customer.Id,
            branch.Id,
            creator.Id,
            BudgetStatus.ClientApproved
        );
        var offer = await CreateOfferAsync(
            context,
            budget.Id,
            customer.Id,
            creator.Id,
            OfferStatus.ClientAccepted
        );
        var service = ServiceFactory.CreateProjectService(context);

        var created = await service.CreateProjectAsync(
            BuildCreateDto(offer.Id, branch.Id),
            creator.Id
        );
        await service.IncrementWeekCounterAsync(created.Id);

        var act = async () => await service.DeleteAsync(created.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task DeleteAsync_WithCurrentDirectExpenses_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-project-delete-directexpenses@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var creator = await TestUserFactory.CreateAsync(
            context,
            "gm-project-delete-directexpenses@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateBudgetAsync(
            context,
            customer.Id,
            branch.Id,
            creator.Id,
            BudgetStatus.ClientApproved
        );
        var offer = await CreateOfferAsync(
            context,
            budget.Id,
            customer.Id,
            creator.Id,
            OfferStatus.ClientAccepted
        );
        var service = ServiceFactory.CreateProjectService(context);

        var created = await service.CreateProjectAsync(
            BuildCreateDto(offer.Id, branch.Id),
            creator.Id
        );

        // No service method exposes this field yet (it'll be populated by MaterialTicket/Payroll/SiteLog
        // in later phases) — set it directly on the entity to exercise the Delete guard defensively.
        var projectEntity = await context.Projects.FindAsync(created.Id);
        projectEntity!.CurrentDirectExpenses = 100m;
        await context.SaveChangesAsync();

        var act = async () => await service.DeleteAsync(created.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task DeleteAsync_WithPendingExpenses_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            "director-project-delete-pendingexpenses@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var creator = await TestUserFactory.CreateAsync(
            context,
            "gm-project-delete-pendingexpenses@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateBudgetAsync(
            context,
            customer.Id,
            branch.Id,
            creator.Id,
            BudgetStatus.ClientApproved
        );
        var offer = await CreateOfferAsync(
            context,
            budget.Id,
            customer.Id,
            creator.Id,
            OfferStatus.ClientAccepted
        );
        var service = ServiceFactory.CreateProjectService(context);

        var created = await service.CreateProjectAsync(
            BuildCreateDto(offer.Id, branch.Id),
            creator.Id
        );

        var projectEntity = await context.Projects.FindAsync(created.Id);
        projectEntity!.PendingExpenses = 50m;
        await context.SaveChangesAsync();

        var act = async () => await service.DeleteAsync(created.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task CreateProjectAsync_TurnKey_AutoCreatesProjectChaptersWithProportionalAssignedSoldTotal()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            $"director-{Guid.NewGuid():N}@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var creator = await TestUserFactory.CreateAsync(
            context,
            $"gm-{Guid.NewGuid():N}@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateBudgetAsync(
            context,
            customer.Id,
            branch.Id,
            creator.Id,
            BudgetStatus.ClientApproved
        );

        var chapter1 = new BudgetChapter
        {
            BudgetId = budget.Id,
            Name = "Cimentación",
            Order = 1,
            TotalChapter = 400m,
            EstimatedWeeks = 4,
            CreatedAt = DateTime.UtcNow,
        };
        var chapter2 = new BudgetChapter
        {
            BudgetId = budget.Id,
            Name = "Estructura",
            Order = 2,
            TotalChapter = 600m,
            EstimatedWeeks = 6,
            CreatedAt = DateTime.UtcNow,
        };
        context.BudgetChapters.AddRange(chapter1, chapter2);
        await context.SaveChangesAsync();

        var offer = await CreateOfferAsync(
            context,
            budget.Id,
            customer.Id,
            creator.Id,
            OfferStatus.ClientAccepted
        ); // OfferType.Turnkey, TotalProjectPrice = 100000m
        var service = ServiceFactory.CreateProjectService(context);

        var project = await service.CreateProjectAsync(
            BuildCreateDto(offer.Id, branch.Id),
            creator.Id
        );

        var projectChapters = await context
            .ProjectChapters.Where(pc => pc.ProjectId == project.Id)
            .OrderBy(pc => pc.ChapterId)
            .ToListAsync();

        projectChapters.Should().HaveCount(2);
        // budget.TotalBudget = 1000 (CreateBudgetAsync); weights 400/1000 and 600/1000 of TotalProjectPrice = 100000.
        projectChapters
            .Should()
            .ContainSingle(pc => pc.ChapterId == chapter1.Id && pc.AssignedSoldTotal == 40000m);
        projectChapters
            .Should()
            .ContainSingle(pc => pc.ChapterId == chapter2.Id && pc.AssignedSoldTotal == 60000m);
    }

    [Fact]
    public async Task CreateProjectAsync_Percentage_AssignedSoldTotalStartsAtZero()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(
            context,
            $"director-{Guid.NewGuid():N}@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var creator = await TestUserFactory.CreateAsync(
            context,
            $"gm-{Guid.NewGuid():N}@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateBudgetAsync(
            context,
            customer.Id,
            branch.Id,
            creator.Id,
            BudgetStatus.ClientApproved
        );

        var chapter = new BudgetChapter
        {
            BudgetId = budget.Id,
            Name = "Cimentación",
            Order = 1,
            TotalChapter = 400m,
            EstimatedWeeks = 4,
            CreatedAt = DateTime.UtcNow,
        };
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
            CreatedAt = DateTime.UtcNow,
        };
        context.Offers.Add(offer);
        await context.SaveChangesAsync();

        var service = ServiceFactory.CreateProjectService(context);
        var project = await service.CreateProjectAsync(
            BuildCreateDto(offer.Id, branch.Id),
            creator.Id
        );

        var projectChapters = await context
            .ProjectChapters.Where(pc => pc.ProjectId == project.Id)
            .ToListAsync();
        projectChapters
            .Should()
            .ContainSingle(pc => pc.ChapterId == chapter.Id && pc.AssignedSoldTotal == 0m);
    }
}
