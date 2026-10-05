using LvDomain.Entities.Commercial;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Commercial;

public class InvoiceDetailConfiguration : IEntityTypeConfiguration<InvoiceDetail>
{
    public void Configure(EntityTypeBuilder<InvoiceDetail> builder)
    {
        builder.ToTable("invoice_details");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Quantity).HasColumnType("decimal(18,2)");
        builder.Property(d => d.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Property(d => d.Subtotal).HasColumnType("decimal(18,2)");

        builder
            .HasOne(d => d.Invoice)
            .WithMany(i => i.Details)
            .HasForeignKey(d => d.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(d => d.Product)
            .WithMany()
            .HasForeignKey(d => d.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
