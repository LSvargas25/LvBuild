using LvDomain.Entities.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Payroll;

public class PayrollDetailConfiguration : IEntityTypeConfiguration<PayrollDetail>
{
    public void Configure(EntityTypeBuilder<PayrollDetail> builder)
    {
        builder.ToTable("payroll_details");

        builder.HasKey(d => d.Id);

        // Calendar date without time of day: PostgreSQL "date", not timestamptz.
        builder.Property(d => d.Date).HasColumnType("date");

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
