using LvDomain.Entities.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Projects;

public class ProjectWorkerConfiguration : IEntityTypeConfiguration<ProjectWorker>
{
    public void Configure(EntityTypeBuilder<ProjectWorker> builder)
    {
        builder.ToTable("project_workers");

        builder.HasKey(pw => pw.Id);

        builder
            .HasOne(pw => pw.Project)
            .WithMany(p => p.Workers)
            .HasForeignKey(pw => pw.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(pw => pw.ProjectId);

        builder
            .HasOne(pw => pw.Worker)
            .WithMany()
            .HasForeignKey(pw => pw.WorkerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(pw => pw.WorkerId);

        builder
            .HasOne(pw => pw.AssignedByUser)
            .WithMany()
            .HasForeignKey(pw => pw.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(pw => pw.AssignedByUserId);
    }
}
