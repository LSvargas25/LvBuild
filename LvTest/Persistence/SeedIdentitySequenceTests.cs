using FluentAssertions;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LvTest.Persistence;

/// <summary>
/// HasData inserts explicit Ids, which do not advance a PostgreSQL identity sequence. If a seed
/// Id is at or above the sequence's start value, the first real insert collides with it
/// (duplicate key) in production while every InMemory test still passes. Builds the Npgsql
/// model without connecting to a database.
/// </summary>
public class SeedIdentitySequenceTests
{
    [Fact]
    public void SeededIds_AreBelowTheIdentityStartValueOfTheirTable()
    {
        using var context = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql("Host=model-only")
                .UseSnakeCaseNamingConvention()
                .Options
        );
        // Seed data is only kept on the design-time model.
        var model = context.GetService<IDesignTimeModel>().Model;

        var checkedTables = new List<string>();
        foreach (var entityType in model.GetEntityTypes())
        {
            var seeds = entityType.GetSeedData().ToList();
            var key = entityType.FindPrimaryKey();
            if (seeds.Count == 0 || key is null || key.Properties.Count != 1)
                continue; // composite keys (e.g. user_roles) have no identity column

            var id = key.Properties[0];
            if (id.ValueGenerated != ValueGenerated.OnAdd)
                continue;

            var maxSeededId = seeds.Max(s =>
                Convert.ToInt64(s[id.Name], System.Globalization.CultureInfo.InvariantCulture)
            );
            var startValue = id.GetIdentityStartValue() ?? 1;

            startValue
                .Should()
                .BeGreaterThan(
                    maxSeededId,
                    $"table '{entityType.GetTableName()}' seeds Id {maxSeededId} but its identity "
                        + $"starts at {startValue}; raise HasIdentityOptions(startValue: ...)"
                );
            checkedTables.Add(entityType.GetTableName()!);
        }

        checkedTables.Should().Contain(["users", "roles"]);
    }
}
