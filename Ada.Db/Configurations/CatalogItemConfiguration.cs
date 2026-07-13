using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ada.Db.Models.Catalog.Items;

namespace Ada.Db.Configurations;

public class CatalogItemConfiguration : IEntityTypeConfiguration<CatalogItem>
{
    public void Configure(EntityTypeBuilder<CatalogItem> entity)
    {
        entity.HasMany(c => c.FurnitureItems)
            .WithMany(f => f.CatalogItems);
    }
}

