using LvDomain.Entities.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Projects;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(p => p.Id);

        // Calendar dates without time of day: PostgreSQL "date", not timestamptz.
        builder.Property(p => p.StartDate).HasColumnType("date");
        builder.Property(p => p.EndDate).HasColumnType("date");

        builder.Property(p => p.ProjectType).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(p => p.TotalWorkedHours).HasColumnType("decimal(18,2)");
        builder.Property(p => p.CurrentDirectExpenses).HasColumnType("decimal(18,2)");
        builder.Property(p => p.PendingExpenses).HasColumnType("decimal(18,2)");
        builder.Property(p => p.CurrentProfit).HasColumnType("decimal(18,2)");

        builder
            .HasOne(p => p.Offer)
            .WithMany()
            .HasForeignKey(p => p.OfferId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.OfferId).IsUnique();

        builder
            .HasOne(p => p.Budget)
            .WithMany()
            .HasForeignKey(p => p.BudgetId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.BudgetId);

        builder
            .HasOne(p => p.Customer)
            .WithMany()
            .HasForeignKey(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.CustomerId);

        builder
            .HasOne(p => p.Branch)
            .WithMany()
            .HasForeignKey(p => p.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.BranchId);

        builder
            .HasOne(p => p.CreatedByUser)
            .WithMany()
            .HasForeignKey(p => p.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.CreatedByUserId);
    }
}
