using LvDomain.Entities.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Payroll;

public class PayrollDetailConfiguration : IEntityTypeConfiguration<PayrollDetail>
{
    public void Configure(EntityTypeBuilder<PayrollDetail> builder)
    {
        builder.ToTable("PayrollDetails");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.HoursWorked).HasColumnType("decimal(5,2)");
        builder.Property(d => d.HourlyRate).HasColumnType("decimal(18,2)");
        builder.Property(d => d.AdvanceAmountApplied).HasColumnType("decimal(18,2)");
        builder.Property(d => d.FinalAmountToPay).HasColumnType("decimal(18,2)");

        builder.Property(d => d.PaymentType).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder
            .HasOne(d => d.Payroll)
            .WithMany(p => p.Details)
            .HasForeignKey(d => d.PayrollId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(d => d.PayrollId);

        builder
            .HasOne(d => d.Worker)
            .WithMany()
            .HasForeignKey(d => d.WorkerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(d => d.WorkerId);
    }
}
