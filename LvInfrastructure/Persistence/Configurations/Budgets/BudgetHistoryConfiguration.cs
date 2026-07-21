using LvDomain.Entities.Budgets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Budgets;

public class BudgetHistoryConfiguration : IEntityTypeConfiguration<BudgetHistory>
{
    public void Configure(EntityTypeBuilder<BudgetHistory> builder)
    {
        builder.ToTable("BudgetHistories");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.PreviousStatus)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(h => h.NewStatus)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(h => h.Comment)
            .HasMaxLength(1000);

        builder.Property(h => h.Reason)
            .HasMaxLength(500);

        builder.HasOne(h => h.Budget)
            .WithMany(b => b.History)
            .HasForeignKey(h => h.BudgetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.User)
            .WithMany()
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
