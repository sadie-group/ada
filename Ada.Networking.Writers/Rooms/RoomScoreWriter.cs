using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms;

[PacketId(ServerPacketId.RoomScore)]
public class RoomScoreWriter : AbstractPacketWriter
{
    public required int Score { get; init; }
    public required bool CanUpvote { get; init; }
}