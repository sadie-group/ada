using Ada.API;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Furniture;

[PacketId(ServerPacketId.RoomWallFurnitureItemRemoved)]
public class RoomWallFurnitureItemRemovedWriter : AbstractPacketWriter
{
    public required PlayerFurnitureItemPlacementDataDto Item { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteString(Item.Id.ToString());
        writer.WriteLong(Item.PlayerFurnitureItem.PlayerId);
    }
}