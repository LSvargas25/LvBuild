using LvDomain.Entities.Auth;
using LvDomain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Auth;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        // HasData inserts explicit Ids, which do not advance a PostgreSQL identity sequence;
        // start after the highest seeded Id so the first real insert does not collide.
        builder.Property(u => u.Id).HasIdentityOptions(startValue: 2);

        builder.Property(u => u.Name).IsRequired().HasMaxLength(150);

        builder.Property(u => u.Email).IsRequired().HasMaxLength(150);

        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash).IsRequired();

        builder.Property(u => u.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(u => u.ProfilePhotoPath).HasMaxLength(300);

        builder.HasData(
            new User
            {
                Id = 1,
                Name = "Administrador",
                Email = "admin@lvconstrucciones.com",
                PasswordHash = "$2a$11$Rj4e9thsbsIFJ6zUWhMyMOCaOkGouPh8JtTuqCv7t1dPj/ilsk4LW",
                Status = UserStatus.Active,
                FailedLoginAttempts = 0,
                CreatedAt = new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc),
            }
        );
    }
}
