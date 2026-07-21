using LvDomain.Entities.Incidents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Incidents;

public class IncidentWorkerConfiguration : IEntityTypeConfiguration<IncidentWorker>
{
    public void Configure(EntityTypeBuilder<IncidentWorker> builder)
    {
        builder.ToTable("IncidentWorkers");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.HoursUsed).HasColumnType("decimal(5,2)");

        builder.HasOne(w => w.Incident)
            .WithMany(i => i.Workers)
            .HasForeignKey(w => w.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(w => w.IncidentId);

        builder.HasOne(w => w.Worker)
            .WithMany()
            .HasForeignKey(w => w.WorkerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(w => w.WorkerId);
    }
}
