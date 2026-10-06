using FluentAssertions;
using LvApplication.DTOs.Auth;
using LvApplication.Services.Auth;
using LvDomain.Enums;
using LvInfrastructure.Persistence;
using LvInfrastructure.Seeding;
using LvTest.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LvTest.Seeding;

/// <summary>
/// The demo seeder runs through the real DI registrations and application services on
/// InMemory; DemoDataSeederPostgresTests repeats the idempotency check on PostgreSQL.
/// </summary>
public class DemoDataSeederTests
{
    [Fact]
    public async Task SeedAsync_RunTwice_SecondRunChangesNothing()
    {
        await using var provider = AppServicesFactory.CreateInMemory();

        (await SeedAsync(provider)).Should().BeTrue();
        var afterFirstRun = await CountRowsAsync(provider);

        (await SeedAsync(provider)).Should().BeFalse();
        var afterSecondRun = await CountRowsAsync(provider);

        afterSecondRun.Should().Equal(afterFirstRun);
        afterFirstRun.Values.Should().OnlyContain(count => count > 0);
    }

    [Fact]
    public async Task SeedAsync_CreatesAnActiveProjectWithRealBudgetVsActualNumbers()
    {
        await using var provider = AppServicesFactory.CreateInMemory();
        await SeedAsync(provider);

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var project = await context.Projects.SingleAsync();
        project.Status.Should().Be(ProjectStatus.Active);
        project.CurrentDirectExpenses.Should().BeGreaterThan(0);
        project.TotalWorkedHours.Should().BeGreaterThan(0);

        var chapters = await context
            .ProjectChapters.Where(c => c.ProjectId == project.Id)
            .ToListAsync();
        chapters.Should().HaveCount(3);
        chapters.Should().OnlyContain(c => c.AssignedSoldTotal > 0);
        chapters.Count(c => c.ActualCostTotal > 0).Should().BeGreaterThanOrEqualTo(2);

        (await context.Payrolls.CountAsync(p => p.Status == PayrollStatus.Paid)).Should().Be(2);
        (await context.Payrolls.CountAsync(p => p.Status == PayrollStatus.Pending)).Should().Be(1);
        (await context.SiteLogs.CountAsync(s => s.Status == SiteLogStatus.Approved)).Should().Be(4);
        (await context.Incidents.CountAsync(i => i.Status == IncidentStatus.Approved))
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task SeedAsync_CoversEveryBudgetStateStoreAndWarehouseFlows()
    {
        await using var provider = AppServicesFactory.CreateInMemory();
        await SeedAsync(provider);

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        (await context.Budgets.Select(b => b.Status).Distinct().ToListAsync())
            .Should()
            .BeEquivalentTo(Enum.GetValues<BudgetStatus>());
        (await context.Offers.SingleAsync()).Status.Should().Be(OfferStatus.ClientAccepted);

        var invoices = await context.Invoices.Include(i => i.Payments).ToListAsync();
        invoices.Should().HaveCount(3).And.OnlyContain(i => i.Status == InvoiceStatus.Issued);
        invoices.Count(i => i.Payments.Sum(p => p.Amount) == i.Total).Should().Be(2);

        (
            await context.InventoryMovements.CountAsync(m =>
                m.Status == InventoryMovementStatus.Accepted
            )
        )
            .Should()
            .BeGreaterThan(0);
        (await context.BranchInventories.AnyAsync(i => i.Quantity > 0)).Should().BeTrue();
        (await context.Branches.CountAsync()).Should().Be(3);
    }

    [Fact]
    public async Task SeedAsync_EveryDemoAccountCanLogInWithTheDocumentedPassword()
    {
        await using var provider = AppServicesFactory.CreateInMemory();
        await SeedAsync(provider);

        foreach (var account in DemoDataSeeder.Accounts)
        {
            await using var scope = provider.CreateAsyncScope();
            var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

            var result = await auth.LoginAsync(
                new LoginRequestDto
                {
                    Email = account.Email,
                    Password = DemoDataSeeder.DemoPassword,
                }
            );

            result.Roles.Should().Equal(account.Role);
        }
    }

    private static async Task<bool> SeedAsync(IServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();
        return await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync();
    }

    internal static async Task<Dictionary<string, int>> CountRowsAsync(IServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var c = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return new Dictionary<string, int>
        {
            ["users"] = await c.Users.CountAsync(),
            ["branches"] = await c.Branches.CountAsync(),
            ["customers"] = await c.Customers.CountAsync(),
            ["suppliers"] = await c.Suppliers.CountAsync(),
            ["workers"] = await c.Workers.CountAsync(),
            ["products"] = await c.Products.CountAsync(),
            ["branch_inventories"] = await c.BranchInventories.CountAsync(),
            ["inventory_movements"] = await c.InventoryMovements.CountAsync(),
            ["invoices"] = await c.Invoices.CountAsync(),
            ["budgets"] = await c.Budgets.CountAsync(),
            ["offers"] = await c.Offers.CountAsync(),
            ["projects"] = await c.Projects.CountAsync(),
            ["material_tickets"] = await c.MaterialTickets.CountAsync(),
            ["site_logs"] = await c.SiteLogs.CountAsync(),
            ["payrolls"] = await c.Payrolls.CountAsync(),
            ["incidents"] = await c.Incidents.CountAsync(),
        };
    }
}
