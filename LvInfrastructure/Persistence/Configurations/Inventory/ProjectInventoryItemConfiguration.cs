using LvDomain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Inventory;

public class ProjectInventoryItemConfiguration : IEntityTypeConfiguration<ProjectInventoryItem>
{
    public void Configure(EntityTypeBuilder<ProjectInventoryItem> builder)
    {
        builder.ToTable("ProjectInventoryItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.CurrentQuantity).HasColumnType("decimal(18,2)");
        builder.Property(i => i.ReferenceUnitCost).HasColumnType("decimal(18,2)");

        builder.HasOne(i => i.Project)
            .WithMany()
            .HasForeignKey(i => i.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Material)
            .WithMany()
            .HasForeignKey(i => i.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(i => i.MaterialId);

        builder.HasIndex(i => new { i.ProjectId, i.MaterialId }).IsUnique();
    }
}
