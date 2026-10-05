using LvDomain.Entities.Budgets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Budgets;

public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("budgets");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name).IsRequired().HasMaxLength(300);

        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.Property(b => b.UtilityPercentage).HasColumnType("decimal(5,2)");
        builder.Property(b => b.IndirectCostsTotal).HasColumnType("decimal(18,2)");
        builder.Property(b => b.TotalBudget).HasColumnType("decimal(18,2)");

        builder
            .HasOne(b => b.Customer)
            .WithMany()
            .HasForeignKey(b => b.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(b => b.Branch)
            .WithMany()
            .HasForeignKey(b => b.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(b => b.CreatedByUser)
            .WithMany()
            .HasForeignKey(b => b.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
