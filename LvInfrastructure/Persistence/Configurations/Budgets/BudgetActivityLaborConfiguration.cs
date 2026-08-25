using LvDomain.Entities.Budgets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Budgets;

public class BudgetActivityLaborConfiguration : IEntityTypeConfiguration<BudgetActivityLabor>
{
    public void Configure(EntityTypeBuilder<BudgetActivityLabor> builder)
    {
        builder.ToTable("BudgetActivityLabor");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.WorkerType).HasConversion<string>().HasMaxLength(50).IsRequired();

        builder.Property(l => l.HourlyRate).HasColumnType("decimal(18,2)");

        builder
            .HasOne(l => l.Activity)
            .WithMany(a => a.Labor)
            .HasForeignKey(l => l.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
