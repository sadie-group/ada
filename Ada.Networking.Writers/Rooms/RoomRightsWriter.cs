using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms;

[PacketId(ServerPacketId.RoomRights)]
public class RoomRightsWriter : AbstractPacketWriter
{
    public required int ControllerLevel { get; init; }
}