using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Users.Trading;

[PacketId(ServerPacketId.RoomUserTradeStatus)]
public class RoomUserTradeStatusWriter : AbstractPacketWriter
{
    public required long UserId { get; set; }
    public required int Status { get; set; }
}