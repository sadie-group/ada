using Ada.API;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Furniture;

[PacketId(ServerPacketId.RoomWallFurnitureItemPlaced)]
public class RoomWallFurnitureItemPlacedWriter : AbstractPacketWriter
{
    public required PlayerFurnitureItemPlacementDataDto RoomFurnitureItem { get; init; }
    public required string OwnerUsername { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        var furnitureItem = RoomFurnitureItem.PlayerFurnitureItem.FurnitureItem;
        
        writer.WriteString(RoomFurnitureItem.Id + "");
        writer.WriteInteger(furnitureItem.AssetId);
        writer.WriteString(RoomFurnitureItem.WallPosition ?? "");
        writer.WriteString(RoomFurnitureItem.PlayerFurnitureItem.MetaData);
        writer.WriteInteger(-1);
        writer.WriteInteger(furnitureItem.InteractionModes > 1 ? 1 : 0);
        writer.WriteLong(RoomFurnitureItem.Id);
        writer.WriteString(OwnerUsername);
    }
}