using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Rights;

[PacketId(ServerPacketId.RoomGiveUserRights)]
public class RoomGiveUserRightsWriter : AbstractPacketWriter
{
    public required int RoomId { get; init; }
    public required int PlayerId { get; init; }
    public required string PlayerUsername { get; init; }
}