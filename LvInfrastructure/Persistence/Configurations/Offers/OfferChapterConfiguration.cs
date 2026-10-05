using LvDomain.Entities.Offers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Offers;

public class OfferChapterConfiguration : IEntityTypeConfiguration<OfferChapter>
{
    public void Configure(EntityTypeBuilder<OfferChapter> builder)
    {
        builder.ToTable("offer_chapters");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ChapterName).IsRequired().HasMaxLength(150);

        builder.Property(c => c.ApproxMaterialQuantity).HasColumnType("decimal(18,2)");

        builder
            .HasOne(c => c.Offer)
            .WithMany(o => o.Chapters)
            .HasForeignKey(c => c.OfferId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
