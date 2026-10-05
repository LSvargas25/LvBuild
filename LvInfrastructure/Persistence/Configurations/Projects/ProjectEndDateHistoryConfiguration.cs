using LvDomain.Entities.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Projects;

public class ProjectEndDateHistoryConfiguration : IEntityTypeConfiguration<ProjectEndDateHistory>
{
    public void Configure(EntityTypeBuilder<ProjectEndDateHistory> builder)
    {
        builder.ToTable("project_end_date_histories");

        builder.HasKey(h => h.Id);

        // Calendar dates without time of day: PostgreSQL "date", not timestamptz.
        builder.Property(h => h.PreviousDate).HasColumnType("date");
        builder.Property(h => h.NewDate).HasColumnType("date");

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
