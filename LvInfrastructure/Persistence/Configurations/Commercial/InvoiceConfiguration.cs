using LvDomain.Entities.Commercial;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Commercial;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber).HasMaxLength(30);
        builder.Property(i => i.PaymentType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(i => i.Subtotal).HasColumnType("decimal(18,2)");
        builder.Property(i => i.Tax).HasColumnType("decimal(18,2)");
        builder.Property(i => i.Total).HasColumnType("decimal(18,2)");

        builder.HasOne(i => i.Branch).WithMany().HasForeignKey(i => i.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.CashRegister).WithMany().HasForeignKey(i => i.CashRegisterId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Customer).WithMany().HasForeignKey(i => i.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.CreatedByUser).WithMany().HasForeignKey(i => i.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.BranchId, i.InvoiceNumber })
            .IsUnique()
            .HasFilter("[InvoiceNumber] IS NOT NULL");
    }
}
