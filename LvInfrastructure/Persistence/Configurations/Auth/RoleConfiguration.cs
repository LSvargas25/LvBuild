using LvDomain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Auth;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id);

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
