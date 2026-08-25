using LvDomain.Entities.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Projects;

public class ProjectEndDateHistoryConfiguration : IEntityTypeConfiguration<ProjectEndDateHistory>
{
    public void Configure(EntityTypeBuilder<ProjectEndDateHistory> builder)
    {
        builder.ToTable("ProjectEndDateHistories");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Reason).IsRequired().HasMaxLength(500);

        builder
            .HasOne(h => h.Project)
            .WithMany(p => p.EndDateHistory)
            .HasForeignKey(h => h.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(h => h.ProjectId);

        builder
            .HasOne(h => h.User)
            .WithMany()
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(h => h.UserId);
    }
}
