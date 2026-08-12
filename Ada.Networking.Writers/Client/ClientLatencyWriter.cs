using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Client;

[PacketId(ServerPacketId.ClientLatency)]
public class ClientLatencyWriter : AbstractPacketWriter
{
    public required int Timestamp { get; init; }
}
