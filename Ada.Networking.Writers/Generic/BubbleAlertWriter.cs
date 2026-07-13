using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Generic;

[PacketId(ServerPacketId.BubbleAlert)]
public class BubbleAlertWriter : AbstractPacketWriter
{
    public required string Key { get; init; }
    public required Dictionary<string, string> Messages { get; init; }
}