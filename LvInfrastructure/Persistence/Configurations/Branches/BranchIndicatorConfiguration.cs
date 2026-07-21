using LvDomain.Entities.Branches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Branches;

public class BranchIndicatorConfiguration : IEntityTypeConfiguration<BranchIndicator>
{
    public void Configure(EntityTypeBuilder<BranchIndicator> builder)
    {
        builder.ToTable("BranchIndicators");

        builder.HasKey(bi => bi.Id);

        builder.Property(bi => bi.Profit).HasColumnType("decimal(18,2)");
        builder.Property(bi => bi.Losses).HasColumnType("decimal(18,2)");
        builder.Property(bi => bi.DirectExpenses).HasColumnType("decimal(18,2)");
        builder.Property(bi => bi.IndirectExpenses).HasColumnType("decimal(18,2)");

        builder.HasOne(bi => bi.Branch)
            .WithOne(b => b.Indicator)
            .HasForeignKey<BranchIndicator>(bi => bi.BranchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(bi => bi.BranchId)
            .IsUnique();
    }
}
