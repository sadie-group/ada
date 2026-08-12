using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Furniture;

[PacketId(ServerPacketId.RoomFloorFurnitureItemPlaced)]
public class RoomFloorItemPlacedWriter : AbstractPacketWriter
{
    public required long Id { get; init; }
    public required int AssetId { get; init; }
    public required int PositionX { get; init; }
    public required int PositionY { get; init; }
    public required int Direction { get; init; }
    public required double PositionZ { get; init; }
    public required string StackHeight { get; init; }
    public required int Extra { get; init; }
    public required int ObjectDataKey { get; init; }
    public required Dictionary<string, string> ObjectData { get; init; }
    public required string MetaData { get; init; }
    public required int Expires { get; init; }
    public required int InteractionModes { get; init; }
    public required long OwnerId { get; init; }
    public required string OwnerUsername { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteLong(Id);
        writer.WriteInteger(AssetId);
        writer.WriteInteger(PositionX);
        writer.WriteInteger(PositionY);
        writer.WriteInteger(Direction);
        writer.WriteString(PositionZ.ToString("0.00"));
        writer.WriteString(StackHeight ?? "");
        writer.WriteInteger(Extra);
        writer.WriteInteger(ObjectDataKey);

        if (ObjectDataKey == (int) Core.Enums.Miscellaneous.ObjectDataKey.LegacyKey)
        {
            writer.WriteString(MetaData);
        }
        else
        {
            writer.WriteInteger(ObjectData.Count);

            foreach (var pair in ObjectData)
            {
                writer.WriteString(pair.Key);
                writer.WriteString(pair.Value);
            }
        }

        writer.WriteInteger(Expires);
        writer.WriteInteger(InteractionModes > 1 ? 1 : 0);
        writer.WriteLong(OwnerId);
        writer.WriteString(OwnerUsername ?? "");
    }
}