using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Navigator;

[PacketId(ServerPacketId.RoomCreated)]
public class RoomCreatedWriter : AbstractPacketWriter
{
    public required int Id { get; init; }
    public required string Name { get; init; }
}