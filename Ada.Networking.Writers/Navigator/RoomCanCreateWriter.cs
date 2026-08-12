using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Navigator;

[PacketId(ServerPacketId.RoomCanCreate)]
public class RoomCanCreateWriter : AbstractPacketWriter
{
    public required bool AtLimit { get; init; }
    public required int RoomLimit { get; init; }
}
