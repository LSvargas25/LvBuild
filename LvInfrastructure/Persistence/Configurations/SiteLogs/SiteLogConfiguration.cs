using LvDomain.Entities.SiteLogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.SiteLogs;

public class SiteLogConfiguration : IEntityTypeConfiguration<SiteLog>
{
    public void Configure(EntityTypeBuilder<SiteLog> builder)
    {
        builder.ToTable("SiteLogs");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.TaskDescription).IsRequired();

        builder.Property(s => s.TotalPayroll).HasColumnType("decimal(18,2)");
        builder.Property(s => s.TotalMaterials).HasColumnType("decimal(18,2)");
        builder.Property(s => s.ProgressPercentage).HasColumnType("decimal(5,2)");

        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder
            .HasOne(s => s.Project)
            .WithMany()
            .HasForeignKey(s => s.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(s => s.CreatedByUser)
            .WithMany()
            .HasForeignKey(s => s.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(s => s.CreatedByUserId);

        builder
            .HasOne(s => s.ApprovedByUser)
            .WithMany()
            .HasForeignKey(s => s.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(s => s.ApprovedByUserId);

        builder.HasIndex(s => new { s.ProjectId, s.WeekStart }).IsUnique();

        builder
            .HasOne(s => s.Chapter)
            .WithMany()
            .HasForeignKey(s => s.ChapterId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(s => s.ChapterId);
    }
}
