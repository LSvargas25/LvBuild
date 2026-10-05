using LvDomain.Entities.Budgets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Budgets;

public class BudgetActivityMaterialConfiguration : IEntityTypeConfiguration<BudgetActivityMaterial>
{
    public void Configure(EntityTypeBuilder<BudgetActivityMaterial> builder)
    {
        builder.ToTable("budget_activity_materials");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.UnitPrice).HasColumnType("decimal(18,2)");

        builder
            .HasOne(m => m.Activity)
            .WithMany(a => a.Materials)
            .HasForeignKey(m => m.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(m => m.Material)
            .WithMany()
            .HasForeignKey(m => m.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
