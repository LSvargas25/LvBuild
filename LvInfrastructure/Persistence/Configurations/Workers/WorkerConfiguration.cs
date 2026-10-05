using LvDomain.Entities.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Workers;

public class WorkerConfiguration : IEntityTypeConfiguration<Worker>
{
    public void Configure(EntityTypeBuilder<Worker> builder)
    {
        builder.ToTable("workers");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name).IsRequired().HasMaxLength(200);

        builder.Property(w => w.PersonalId).HasMaxLength(30);

        builder.Property(w => w.PhoneNumber).HasMaxLength(30);

        builder.Property(w => w.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(w => w.Category).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.Property(w => w.Type).HasConversion<string>().HasMaxLength(50).IsRequired();

        builder.Property(w => w.HourlyRate).HasColumnType("decimal(18,2)");

        builder.HasIndex(w => w.PersonalId);

        builder
            .HasOne(w => w.Branch)
            .WithMany()
            .HasForeignKey(w => w.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(w => w.BranchId);
    }
}
