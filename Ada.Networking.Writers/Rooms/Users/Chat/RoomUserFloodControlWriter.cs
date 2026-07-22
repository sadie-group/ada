using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Users.Chat;

[PacketId(ServerPacketId.RoomUserFloodControl)]
public class RoomUserFloodControlWriter : AbstractPacketWriter
{
    public required int Seconds { get; init; }
}
