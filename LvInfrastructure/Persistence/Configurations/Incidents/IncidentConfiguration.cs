using LvDomain.Entities.Incidents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Incidents;

public class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("incidents");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Description).IsRequired();
        builder.Property(i => i.TotalCost).HasColumnType("decimal(18,2)");

        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder
            .HasOne(i => i.Project)
            .WithMany()
            .HasForeignKey(i => i.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(i => i.CreatedByUser)
            .WithMany()
            .HasForeignKey(i => i.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(i => i.ApprovedByUser)
            .WithMany()
            .HasForeignKey(i => i.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(i => i.Chapter)
            .WithMany()
            .HasForeignKey(i => i.ChapterId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(i => i.ChapterId);
    }
}
