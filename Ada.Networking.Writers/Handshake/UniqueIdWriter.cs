using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Handshake;

[PacketId(ServerPacketId.UniqueId)]
public class UniqueIdWriter : AbstractPacketWriter
{
    public required string MachineId { get; set; }
}