using LvDomain.Entities.Commercial;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Commercial;

public class ProductIncorporationTicketConfiguration : IEntityTypeConfiguration<ProductIncorporationTicket>
{
    public void Configure(EntityTypeBuilder<ProductIncorporationTicket> builder)
    {
        builder.ToTable("ProductIncorporationTickets");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Quantity).HasColumnType("decimal(18,2)");
        builder.Property(t => t.UnitCost).HasColumnType("decimal(18,2)");
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne(t => t.Branch).WithMany().HasForeignKey(t => t.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => t.BranchId);

        builder.HasOne(t => t.Product).WithMany().HasForeignKey(t => t.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => t.ProductId);

        builder.HasOne(t => t.Supplier).WithMany().HasForeignKey(t => t.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => t.SupplierId);

        builder.HasOne(t => t.CreatedByUser).WithMany().HasForeignKey(t => t.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.ValidatedByUser).WithMany().HasForeignKey(t => t.ValidatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
