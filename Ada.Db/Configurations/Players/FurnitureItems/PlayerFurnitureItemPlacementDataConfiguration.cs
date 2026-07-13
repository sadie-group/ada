using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ada.Db.Models.Players.Furniture;

namespace Ada.Db.Configurations.Players.FurnitureItems;

public class PlayerFurnitureItemPlacementDataConfiguration : IEntityTypeConfiguration<PlayerFurnitureItemPlacementData>
{
    public void Configure(EntityTypeBuilder<PlayerFurnitureItemPlacementData> entity)
    {
        entity.ToTable("player_furniture_item_placement_data");

        entity.Navigation(x => x.PlayerFurnitureItem).AutoInclude();
    }
}