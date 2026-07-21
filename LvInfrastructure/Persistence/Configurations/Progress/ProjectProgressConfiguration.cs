using LvDomain.Entities.Progress;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Progress;

public class ProjectProgressConfiguration : IEntityTypeConfiguration<ProjectProgress>
{
    public void Configure(EntityTypeBuilder<ProjectProgress> builder)
    {
        builder.ToTable("ProjectProgresses");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ProgressPercentage).HasColumnType("decimal(5,2)");

        builder.HasOne(p => p.Project)
            .WithMany()
            .HasForeignKey(p => p.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.SiteLog)
            .WithMany()
            .HasForeignKey(p => p.SiteLogId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.SiteLogId).IsUnique();
    }
}
