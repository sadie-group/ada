using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players.Messenger;

[PacketId(ServerPacketId.PlayerMessageError)]
public class PlayerMessageErrorWriter : AbstractPacketWriter
{
    public required int Error { get; init; }
    public required int TargetId { get; init; }
    public string Message { get; init; } = "";
}