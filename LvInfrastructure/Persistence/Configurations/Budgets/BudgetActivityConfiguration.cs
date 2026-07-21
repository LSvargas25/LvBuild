using LvDomain.Entities.Budgets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Budgets;

public class BudgetActivityConfiguration : IEntityTypeConfiguration<BudgetActivity>
{
    public void Configure(EntityTypeBuilder<BudgetActivity> builder)
    {
        builder.ToTable("BudgetActivities");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Description)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(a => a.MaterialQuantity).HasColumnType("decimal(18,2)");
        builder.Property(a => a.MaterialCost).HasColumnType("decimal(18,2)");
        builder.Property(a => a.LaborCost).HasColumnType("decimal(18,2)");
        builder.Property(a => a.EquipmentCost).HasColumnType("decimal(18,2)");
        builder.Property(a => a.TotalActivity).HasColumnType("decimal(18,2)");

        builder.HasOne(a => a.Chapter)
            .WithMany(c => c.Activities)
            .HasForeignKey(a => a.ChapterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
