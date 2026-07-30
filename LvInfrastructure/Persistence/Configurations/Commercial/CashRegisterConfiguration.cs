using LvDomain.Entities.Commercial;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Commercial;

public class CashRegisterConfiguration : IEntityTypeConfiguration<CashRegister>
{
    public void Configure(EntityTypeBuilder<CashRegister> builder)
    {
        builder.ToTable("CashRegisters");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.OpeningBalance).HasColumnType("decimal(18,2)");
        builder.Property(c => c.ClosingBalance).HasColumnType("decimal(18,2)");
        builder.Property(c => c.ExpectedBalance).HasColumnType("decimal(18,2)");
        builder.Property(c => c.Difference).HasColumnType("decimal(18,2)");

        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne(c => c.Branch).WithMany().HasForeignKey(c => c.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(c => c.BranchId);

        builder.HasOne(c => c.OpenedByUser).WithMany().HasForeignKey(c => c.OpenedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.ClosedByUser).WithMany().HasForeignKey(c => c.ClosedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
