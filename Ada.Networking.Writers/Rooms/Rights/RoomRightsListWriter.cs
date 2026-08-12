using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Rights;

[PacketId(ServerPacketId.RoomRightsList)]
public class RoomRightsListWriter : AbstractPacketWriter
{
    public required int RoomId { get; init; }
    public required IReadOnlyList<KeyValuePair<long, string>> Players { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(RoomId);
        writer.WriteInteger(Players.Count);

        foreach (var (playerId, username) in Players)
        {
            writer.WriteInteger((int) playerId);
            writer.WriteString(username);
        }
    }
}
