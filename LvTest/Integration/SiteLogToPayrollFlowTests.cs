using FluentAssertions;
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
using Xunit;

namespace LvTest.Integration;

/// <summary>
/// Exercises Bitacora aprobada -> Planilla pagada against a real SQL Server database.
/// This is the dependency the team called out explicitly (Payroll cannot be created
/// without an Approved SiteLog); worth proving against the real engine, not just InMemory.
/// </summary>
[Collection("SqlServerIntegration")]
public class SiteLogToPayrollFlowTests
{
    [Fact]
    public async Task MarkAsPaidAsync_ForPayrollFromApprovedSiteLog_UpdatesProjectExpensesAgainstRealSqlServer()
    {
        await using var context = await SqlServerTestDbContextFactory.CreateAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var director = await TestUserFactory.CreateAsync(context, $"director-{Guid.NewGuid():N}@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var manager = await TestUserFactory.CreateAsync(context, $"gm-{Guid.NewGuid():N}@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var projectAdmin = await TestUserFactory.CreateAsync(context, $"pa-{Guid.NewGuid():N}@example.com", roleId: TestUserFactory.ProjectAdminRoleId);

        var customer = new Customer
        {
            Name = "Cliente Integracion Planilla",
            CustomerType = CustomerType.Project,
            Status = ActiveStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        context.Customers.Add(customer);

        var branch = new Branch
        {
            Name = "Oficina Integracion Planilla",
            City = "San Jose",
            Province = "San Jose",
            Status = BranchStatus.Active,
            BranchType = BranchType.Office,
            OperationsDirectorId = director.Id,
            CreatedAt = DateTime.UtcNow
        };
        context.Branches.Add(branch);
        await context.SaveChangesAsync();

        var budget = new Budget
        {
            CustomerId = customer.Id,
            BranchId = branch.Id,
            Name = "Edificio Integracion Planilla",
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
            OfferNumber = $"OF-INT-{Guid.NewGuid():N}"[..20],
            OfferType = OfferType.Turnkey,
            IssueDate = new DateTime(2026, 1, 10),
            ValidityDays = 30,
            WorkLocation = "San Jose Centro",
            WorkScope = "Construccion de edificio de integracion",
            EstimatedStartDate = new DateTime(2026, 2, 1),
            EstimatedDurationWeeks = 10,
            EstimatedDeliveryDate = new DateTime(2026, 2, 1).AddDays(10 * 7),
            PaymentTerms = "50% inicio, 50% entrega",
            Warranties = "1 ano estructural",
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

        var worker = new Worker
        {
            Name = "Trabajador Integracion",
            Status = ActiveStatus.Active,
            Category = WorkerCategory.Construction,
            Type = WorkerType.Laborer,
            HourlyRate = 5m,
            CreatedAt = DateTime.UtcNow
        };
        context.Workers.Add(worker);
        await context.SaveChangesAsync();

        var siteLogService = ServiceFactory.CreateSiteLogService(context);
        var siteLog = await siteLogService.CreateAsync(new CreateSiteLogDto
        {
            ProjectId = project.Id,
            WeekStart = new DateTime(2026, 3, 2),
            WeekEnd = new DateTime(2026, 3, 8),
            TaskDescription = "Semana de integracion",
            Workers = new List<SiteLogWorkerDto>(),
            Materials = new List<SiteLogMaterialDto>(),
            Equipment = new List<SiteLogEquipmentDto>()
        }, projectAdmin.Id);

        await siteLogService.SubmitToReviewAsync(siteLog.Id);
        var approvedSiteLog = await siteLogService.ApproveAsync(siteLog.Id, manager.Id);

        var payrollService = ServiceFactory.CreatePayrollService(context);
        const decimal hoursWorked = 8m;
        const decimal hourlyRate = 5m;
        var finalAmount = hoursWorked * hourlyRate;

        var payroll = await payrollService.CreateAsync(new CreatePayrollDto
        {
            SiteLogId = approvedSiteLog.Id,
            Details = new List<PayrollDetailDto>
            {
                new()
                {
                    WorkerId = worker.Id,
                    Date = new DateTime(2026, 3, 2),
                    HoursWorked = hoursWorked,
                    HourlyRate = hourlyRate,
                    PaymentType = PayrollPaymentType.Full,
                    Payments = new List<PayrollDetailPaymentDto>
                    {
                        new() { PaymentMethod = PaymentMethod.Transfer, Amount = finalAmount }
                    }
                }
            }
        }, projectAdmin.Id);

        var paid = await payrollService.MarkAsPaidAsync(payroll.Id);

        paid.Status.Should().Be(PayrollStatus.Paid);

        context.ChangeTracker.Clear();
        var reloadedProject = await context.Projects.AsNoTracking().FirstAsync(p => p.Id == project.Id);
        reloadedProject.CurrentDirectExpenses.Should().Be(finalAmount);

        var reloadedSiteLog = await context.SiteLogs.AsNoTracking().FirstAsync(s => s.Id == siteLog.Id);
        reloadedSiteLog.TotalPayroll.Should().Be(finalAmount);

        await transaction.RollbackAsync();
    }
}
