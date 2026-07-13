using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Users.Trading;

[PacketId(ServerPacketId.RoomUserTradeStarted)]
public class RoomUserTradeStartedWriter : AbstractPacketWriter
{
    public required List<long> UserIds { get; init; }
    public required int State { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        foreach (var id in UserIds)
        {
            writer.WriteLong(id);
            writer.WriteInteger(State);
        }
    }
}