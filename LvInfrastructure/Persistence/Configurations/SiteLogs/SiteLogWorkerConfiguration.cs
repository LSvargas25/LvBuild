using LvDomain.Entities.SiteLogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.SiteLogs;

public class SiteLogWorkerConfiguration : IEntityTypeConfiguration<SiteLogWorker>
{
    public void Configure(EntityTypeBuilder<SiteLogWorker> builder)
    {
        builder.ToTable("SiteLogWorkers");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.HoursWorked).HasColumnType("decimal(5,2)");

        builder
            .HasOne(w => w.SiteLog)
            .WithMany(s => s.Workers)
            .HasForeignKey(w => w.SiteLogId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(w => w.SiteLogId);

        builder
            .HasOne(w => w.Worker)
            .WithMany()
            .HasForeignKey(w => w.WorkerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(w => w.WorkerId);
    }
}
