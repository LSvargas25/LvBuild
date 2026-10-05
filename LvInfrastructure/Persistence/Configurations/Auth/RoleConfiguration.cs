using LvDomain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Auth;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);

        // HasData inserts explicit Ids, which do not advance a PostgreSQL identity sequence;
        // start after the highest seeded Id so the first real insert does not collide.
        builder.Property(r => r.Id).HasIdentityOptions(startValue: 6);

        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);

        builder.HasIndex(r => r.Name).IsUnique();

        var seedDate = new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(
            new Role
            {
                Id = 1,
                Name = "GeneralManager",
                CreatedAt = seedDate,
            },
            new Role
            {
                Id = 2,
                Name = "OperationsDirector",
                CreatedAt = seedDate,
            },
            new Role
            {
                Id = 3,
                Name = "ProjectAdmin",
                CreatedAt = seedDate,
            },
            new Role
            {
                Id = 4,
                Name = "BranchAdmin",
                CreatedAt = seedDate,
            },
            new Role
            {
                Id = 5,
                Name = "BusinessManager",
                CreatedAt = seedDate,
            }
        );
    }
}
