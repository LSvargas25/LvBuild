using LvDomain.Entities.Offers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Offers;

public class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.ToTable("offers");

        builder.HasKey(o => o.Id);

        // Calendar dates without time of day: PostgreSQL "date", not timestamptz.
        builder.Property(o => o.IssueDate).HasColumnType("date");
        builder.Property(o => o.EstimatedStartDate).HasColumnType("date");
        builder.Property(o => o.EstimatedDeliveryDate).HasColumnType("date");

        builder.Property(o => o.OfferNumber).IsRequired().HasMaxLength(30);

        builder.HasIndex(o => o.OfferNumber).IsUnique();

        builder.Property(o => o.OfferType).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.Property(o => o.PaymentFrequency).HasConversion<string>().HasMaxLength(30);

        builder.Property(o => o.WorkLocation).IsRequired().HasMaxLength(300);
        builder.Property(o => o.WorkScope).IsRequired();
        builder.Property(o => o.PaymentTerms).IsRequired().HasMaxLength(300);
        builder.Property(o => o.Warranties).IsRequired().HasMaxLength(500);
        builder.Property(o => o.Exclusions).IsRequired().HasMaxLength(500);
        builder.Property(o => o.PercentageIncludes).HasMaxLength(1000);
        builder.Property(o => o.PercentageExcludes).HasMaxLength(1000);
        builder.Property(o => o.PercentageCalculationMethod).HasMaxLength(500);

        builder.Property(o => o.TotalProjectPrice).HasColumnType("decimal(18,2)");
        builder.Property(o => o.AgreedPercentage).HasColumnType("decimal(5,2)");

        builder
            .HasOne(o => o.Budget)
            .WithMany()
            .HasForeignKey(o => o.BudgetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(o => o.Customer)
            .WithMany()
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(o => o.CreatedByUser)
            .WithMany()
            .HasForeignKey(o => o.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
