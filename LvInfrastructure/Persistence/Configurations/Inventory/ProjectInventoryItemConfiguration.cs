using LvDomain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Inventory;

public class ProjectInventoryItemConfiguration : IEntityTypeConfiguration<ProjectInventoryItem>
{
    public void Configure(EntityTypeBuilder<ProjectInventoryItem> builder)
    {
        builder.ToTable(
            "project_inventory_items",
            t =>
                t.HasCheckConstraint(
                    "ck_project_inventory_items_exactly_one_catalog_reference",
                    "(material_id IS NOT NULL AND product_id IS NULL) OR (material_id IS NULL AND product_id IS NOT NULL)"
                )
        );

        builder.HasKey(i => i.Id);

        builder.Property(i => i.CurrentQuantity).HasColumnType("decimal(18,2)");
        builder.Property(i => i.ReferenceUnitCost).HasColumnType("decimal(18,2)");

        builder
            .HasOne(i => i.Project)
            .WithMany()
            .HasForeignKey(i => i.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(i => i.Material)
            .WithMany()
            .HasForeignKey(i => i.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(i => i.MaterialId);

        builder
            .HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(i => i.ProductId);

        builder
            .HasIndex(i => new { i.ProjectId, i.MaterialId })
            .IsUnique()
            .HasFilter("material_id IS NOT NULL");
        builder
            .HasIndex(i => new { i.ProjectId, i.ProductId })
            .IsUnique()
            .HasFilter("product_id IS NOT NULL");
    }
}
