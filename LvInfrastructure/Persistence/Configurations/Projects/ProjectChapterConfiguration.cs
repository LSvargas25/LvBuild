using LvDomain.Entities.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Projects;

public class ProjectChapterConfiguration : IEntityTypeConfiguration<ProjectChapter>
{
    public void Configure(EntityTypeBuilder<ProjectChapter> builder)
    {
        builder.ToTable("ProjectChapters");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.AssignedSoldTotal).HasColumnType("decimal(18,2)");
        builder.Property(c => c.ActualCostTotal).HasColumnType("decimal(18,2)");
        builder.Property(c => c.ChapterProfit).HasColumnType("decimal(18,2)");
        builder.Property(c => c.IncidentPercentage).HasColumnType("decimal(5,2)");

        builder
            .HasOne(c => c.Project)
            .WithMany()
            .HasForeignKey(c => c.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(c => c.Chapter)
            .WithMany()
            .HasForeignKey(c => c.ChapterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.ProjectId, c.ChapterId }).IsUnique();
    }
}
