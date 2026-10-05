using LvDomain.Entities.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Payroll;

public class PayrollDetailPaymentConfiguration : IEntityTypeConfiguration<PayrollDetailPayment>
{
    public void Configure(EntityTypeBuilder<PayrollDetailPayment> builder)
    {
        builder.ToTable("payroll_detail_payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount).HasColumnType("decimal(18,2)");

        builder
            .Property(p => p.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder
            .HasOne(p => p.PayrollDetail)
            .WithMany(d => d.Payments)
            .HasForeignKey(p => p.PayrollDetailId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(p => p.PayrollDetailId);
    }
}
