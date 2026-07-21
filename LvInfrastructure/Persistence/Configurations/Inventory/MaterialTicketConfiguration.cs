using LvDomain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Inventory;

public class MaterialTicketConfiguration : IEntityTypeConfiguration<MaterialTicket>
{
    public void Configure(EntityTypeBuilder<MaterialTicket> builder)
    {
        builder.ToTable("MaterialTickets");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.InvoicePhotoPath).HasMaxLength(300);
        builder.Property(t => t.MaterialName).IsRequired().HasMaxLength(200);

        builder.Property(t => t.Quantity).HasColumnType("decimal(18,2)");
        builder.Property(t => t.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Property(t => t.Discount).HasColumnType("decimal(18,2)");
        builder.Property(t => t.Subtotal).HasColumnType("decimal(18,2)");
        builder.Property(t => t.Total).HasColumnType("decimal(18,2)");

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(t => t.Project)
            .WithMany()
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => t.ProjectId);

        builder.HasOne(t => t.Supplier)
            .WithMany()
            .HasForeignKey(t => t.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => t.SupplierId);

        builder.HasOne(t => t.Material)
            .WithMany()
            .HasForeignKey(t => t.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => t.MaterialId);

        builder.HasOne(t => t.CreatedByUser)
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => t.CreatedByUserId);
    }
}
