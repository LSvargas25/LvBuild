using LvDomain.Entities.Commercial;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Commercial;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.Sku).IsRequired().HasMaxLength(50);
        builder.Property(p => p.UnitOfMeasure).HasMaxLength(30);
        builder.Property(p => p.Category).HasMaxLength(100);

        builder.Property(p => p.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Property(p => p.UnitCost).HasColumnType("decimal(18,2)");

        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(p => p.Sku).IsUnique();

        builder
            .HasOne(p => p.CreatedByUser)
            .WithMany()
            .HasForeignKey(p => p.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.CreatedByUserId);

        builder
            .HasOne(p => p.ValidatedByUser)
            .WithMany()
            .HasForeignKey(p => p.ValidatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.ValidatedByUserId);
    }
}
