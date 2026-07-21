using LvDomain.Entities.SiteLogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.SiteLogs;

public class SiteLogEquipmentConfiguration : IEntityTypeConfiguration<SiteLogEquipment>
{
    public void Configure(EntityTypeBuilder<SiteLogEquipment> builder)
    {
        builder.ToTable("SiteLogEquipment");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Description).IsRequired().HasMaxLength(200);

        builder.Property(e => e.EquipmentType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(e => e.SiteLog)
            .WithMany(s => s.Equipment)
            .HasForeignKey(e => e.SiteLogId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.SiteLogId);
    }
}
