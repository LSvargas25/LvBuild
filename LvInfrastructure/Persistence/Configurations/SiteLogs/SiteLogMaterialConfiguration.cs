using LvDomain.Entities.SiteLogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.SiteLogs;

public class SiteLogMaterialConfiguration : IEntityTypeConfiguration<SiteLogMaterial>
{
    public void Configure(EntityTypeBuilder<SiteLogMaterial> builder)
    {
        builder.ToTable("site_log_materials");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.QuantityUsed).HasColumnType("decimal(18,2)");

        builder
            .HasOne(m => m.SiteLog)
            .WithMany(s => s.Materials)
            .HasForeignKey(m => m.SiteLogId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(m => m.SiteLogId);

        builder
            .HasOne(m => m.Material)
            .WithMany()
            .HasForeignKey(m => m.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(m => m.MaterialId);
    }
}
