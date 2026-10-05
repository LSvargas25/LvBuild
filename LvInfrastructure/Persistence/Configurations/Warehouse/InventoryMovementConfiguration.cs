using LvDomain.Entities.Warehouse;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Warehouse;

public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable(
            "inventory_movements",
            t =>
                t.HasCheckConstraint(
                    "ck_inventory_movements_exactly_one_destination",
                    "(destination_branch_id IS NOT NULL AND destination_project_id IS NULL) OR (destination_branch_id IS NULL AND destination_project_id IS NOT NULL)"
                )
        );

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Quantity).HasColumnType("decimal(18,2)");
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder
            .HasOne(m => m.OriginBranch)
            .WithMany()
            .HasForeignKey(m => m.OriginBranchId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(m => m.OriginBranchId);

        builder
            .HasOne(m => m.DestinationBranch)
            .WithMany()
            .HasForeignKey(m => m.DestinationBranchId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(m => m.DestinationBranchId);

        builder
            .HasOne(m => m.DestinationProject)
            .WithMany()
            .HasForeignKey(m => m.DestinationProjectId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(m => m.DestinationProjectId);

        builder
            .HasOne(m => m.Product)
            .WithMany()
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(m => m.ProductId);

        builder
            .HasOne(m => m.SentByUser)
            .WithMany()
            .HasForeignKey(m => m.SentByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(m => m.ValidatedByUser)
            .WithMany()
            .HasForeignKey(m => m.ValidatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
