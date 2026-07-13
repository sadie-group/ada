using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Polls;

[PacketId(ServerPacketId.RoomPollStarted)]
public class RoomPollStartedWriter : AbstractPacketWriter
{
    public required string Question { get; init; }
    public required List<string> Choices { get; init; }
}