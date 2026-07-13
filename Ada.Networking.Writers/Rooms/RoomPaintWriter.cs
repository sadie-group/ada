using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms;

[PacketId(ServerPacketId.RoomPaint)]
public class RoomPaintWriter : AbstractPacketWriter
{
    public required string Type { get; init; }
    public required string Value { get; init; }
}