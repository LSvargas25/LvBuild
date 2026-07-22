using LvDomain.Entities.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Payroll;

public class PayrollConfiguration : IEntityTypeConfiguration<LvDomain.Entities.Payroll.Payroll>
{
    public void Configure(EntityTypeBuilder<LvDomain.Entities.Payroll.Payroll> builder)
    {
        builder.ToTable("Payrolls");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.TotalPayroll).HasColumnType("decimal(18,2)");

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasOne(p => p.Project)
            .WithMany()
            .HasForeignKey(p => p.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.SiteLog)
            .WithMany()
            .HasForeignKey(p => p.SiteLogId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.SiteLogId).IsUnique();

        builder.HasOne(p => p.CreatedByUser)
            .WithMany()
            .HasForeignKey(p => p.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Chapter)
            .WithMany()
            .HasForeignKey(p => p.ChapterId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.ChapterId);
    }
}
