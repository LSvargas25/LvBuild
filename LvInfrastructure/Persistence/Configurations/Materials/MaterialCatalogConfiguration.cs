using LvDomain.Entities.Materials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LvInfrastructure.Persistence.Configurations.Materials;

public class MaterialCatalogConfiguration : IEntityTypeConfiguration<MaterialCatalog>
{
    public void Configure(EntityTypeBuilder<MaterialCatalog> builder)
    {
        builder.ToTable("material_catalogs");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name).IsRequired().HasMaxLength(200);

        builder.Property(m => m.UnitOfMeasure).HasMaxLength(30);

        builder.HasIndex(m => m.Name);
    }
}
