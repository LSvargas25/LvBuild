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
}
