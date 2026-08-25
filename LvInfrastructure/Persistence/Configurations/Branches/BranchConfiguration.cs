using LvDomain.Entities.Branches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Branches;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name).IsRequired().HasMaxLength(150);

        builder.Property(b => b.PhoneNumber).HasMaxLength(30);

        builder.Property(b => b.Email).HasMaxLength(150);

        builder.Property(b => b.City).IsRequired().HasMaxLength(100);

        builder.Property(b => b.Province).IsRequired().HasMaxLength(100);

        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(b => b.BranchType).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder
            .HasOne(b => b.OperationsDirector)
            .WithMany()
            .HasForeignKey(b => b.OperationsDirectorId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(b => b.BranchAdmin)
            .WithMany()
            .HasForeignKey(b => b.BranchAdminId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(b => b.BusinessManager)
            .WithMany()
            .HasForeignKey(b => b.BusinessManagerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
