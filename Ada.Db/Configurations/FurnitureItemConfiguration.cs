using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Shared.Helpers;
using Ada.Db.Models.Furniture;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ada.Db.Configurations;

public class FurnitureItemConfiguration : IEntityTypeConfiguration<FurnitureItem>
{
    public void Configure(EntityTypeBuilder<FurnitureItem> entity)
    {
        entity.Property(e => e.Type)
            .HasConversion(
                v => EnumHelpers.GetEnumDescription(v),
                v => EnumHelpers.GetEnumValueFromDescription<FurnitureItemType>(v));

        entity.Navigation(x => x.HandItems).AutoInclude();

        entity.HasMany(f => f.HandItems)
            .WithMany(h => h.FurnitureItems);
    }
}