using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players;

[PacketId(ServerPacketId.PlayerAlert)]
public class PlayerAlertWriter : AbstractPacketWriter
{
    public required string Message { get; set; }
}