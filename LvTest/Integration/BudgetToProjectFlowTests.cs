using FluentAssertions;
using LvApplication.DTOs.Projects;
using LvDomain.Entities.Branches;
using LvDomain.Entities.Budgets;
using LvDomain.Entities.Customers;
using LvDomain.Entities.Offers;
using LvDomain.Enums;
using LvInfrastructure.Persistence;
using LvTest.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LvTest.Integration;

/// <summary>
/// Exercises Presupuesto -> Oferta -> Proyecto against a real SQL Server database
/// (via the AddWarehouseModule-era schema applied through real migrations), not
/// EF Core InMemory. Catches FK/constraint behavior InMemory can't validate.
/// </summary>
[Collection("SqlServerIntegration")]
public class BudgetToProjectFlowTests
{
    [Fact]
    public async Task CreateProjectAsync_FromApprovedBudgetAndAcceptedOffer_PersistsAgainstRealSqlServer()
    {
        await using var context = await SqlServerTestDbContextFactory.CreateAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var director = await TestUserFactory.CreateAsync(context, $"director-{Guid.NewGuid():N}@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var manager = await TestUserFactory.CreateAsync(context, $"gm-{Guid.NewGuid():N}@example.com", roleId: TestUserFactory.GeneralManagerRoleId);

        var customer = new Customer
        {
            Name = "Cliente Integracion",
            CustomerType = CustomerType.Project,
            Status = ActiveStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var branch = new Branch
        {
            Name = "Oficina Integracion",
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
            Name = "Edificio Integracion",
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

        var result = await projectService.CreateProjectAsync(new CreateProjectDto
        {
            OfferId = offer.Id,
            BranchId = branch.Id,
            StartDate = new DateTime(2026, 3, 1)
        }, manager.Id);

        result.Id.Should().BeGreaterThan(0);
        result.Status.Should().Be(ProjectStatus.Active);

        context.ChangeTracker.Clear();
        var reloaded = await context.Projects
            .AsNoTracking()
            .FirstAsync(p => p.Id == result.Id);

        reloaded.OfferId.Should().Be(offer.Id);
        reloaded.BudgetId.Should().Be(budget.Id);
        reloaded.CustomerId.Should().Be(customer.Id);
        reloaded.BranchId.Should().Be(branch.Id);

        await transaction.RollbackAsync();
    }
}
