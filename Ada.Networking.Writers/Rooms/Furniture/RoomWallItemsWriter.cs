using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Furniture;

[PacketId(ServerPacketId.RoomWallItems)]
public class RoomWallItemsWriter : AbstractPacketWriter
{
    public required Dictionary<long, string> FurnitureOwners { get; init; }
    public required ICollection<PlayerFurnitureItemPlacementDataDto> WallItems { get; init; }

    public override void OnConfigureRules()
    {
        Override(GetType().GetProperty(nameof(WallItems))!, writer =>
        {
            writer.WriteInteger(WallItems.Count);

            foreach (var item in WallItems)
            {
                var furnitureItem = item.PlayerFurnitureItem.FurnitureItem;
                
                writer.WriteString(item.Id + "");
                writer.WriteInteger(furnitureItem.AssetId);
                writer.WriteString(item.WallPosition ?? "");
                writer.WriteString(item.PlayerFurnitureItem.MetaData);
                writer.WriteInteger(-1);
                writer.WriteInteger(furnitureItem.InteractionModes > 1 ? 1 : 0);
                writer.WriteLong(item.PlayerFurnitureItem.PlayerId);
            }
        });
    }
}