using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms;

[PacketId(ServerPacketId.RoomPane)]
public class RoomPaneWriter : AbstractPacketWriter
{
    public required int RoomId { get; init; }
    public required bool Owner { get; init; }
}