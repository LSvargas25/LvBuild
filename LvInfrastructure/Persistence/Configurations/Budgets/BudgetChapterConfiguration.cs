using LvDomain.Entities.Budgets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Budgets;

public class BudgetChapterConfiguration : IEntityTypeConfiguration<BudgetChapter>
{
    public void Configure(EntityTypeBuilder<BudgetChapter> builder)
    {
        builder.ToTable("BudgetChapters");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(150);

        builder.Property(c => c.TotalChapter).HasColumnType("decimal(18,2)");

        builder
            .HasOne(c => c.Budget)
            .WithMany(b => b.Chapters)
            .HasForeignKey(c => c.BudgetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
