using LvDomain.Entities.Incidents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Incidents;

public class IncidentMaterialConfiguration : IEntityTypeConfiguration<IncidentMaterial>
{
    public void Configure(EntityTypeBuilder<IncidentMaterial> builder)
    {
        builder.ToTable("incident_materials");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Quantity).HasColumnType("decimal(18,2)");

        builder
            .HasOne(m => m.Incident)
            .WithMany(i => i.Materials)
            .HasForeignKey(m => m.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(m => m.IncidentId);

        builder
            .HasOne(m => m.Material)
            .WithMany()
            .HasForeignKey(m => m.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(m => m.MaterialId);
    }
}
