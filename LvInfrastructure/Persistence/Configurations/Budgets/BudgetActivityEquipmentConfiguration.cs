using LvDomain.Entities.Budgets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Budgets;

public class BudgetActivityEquipmentConfiguration
    : IEntityTypeConfiguration<BudgetActivityEquipment>
{
    public void Configure(EntityTypeBuilder<BudgetActivityEquipment> builder)
    {
        builder.ToTable("BudgetActivityEquipment");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EquipmentName).IsRequired().HasMaxLength(150);

        builder.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");

        builder
            .HasOne(e => e.Activity)
            .WithMany(a => a.Equipment)
            .HasForeignKey(e => e.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
