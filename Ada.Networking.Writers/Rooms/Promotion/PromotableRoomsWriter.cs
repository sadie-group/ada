using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Promotion;

[PacketId(ServerPacketId.PromotableRooms)]
public class PromotableRoomsWriter : AbstractPacketWriter
{
    public required IReadOnlyList<KeyValuePair<int, string>> Rooms { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(Rooms.Count);

        foreach (var (roomId, name) in Rooms)
        {
            writer.WriteInteger(roomId);
            writer.WriteString(name);
            writer.WriteBool(false);
        }
    }
}
