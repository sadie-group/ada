using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players.Messenger;

[PacketId(ServerPacketId.PlayerStalkError)]
public class PlayerStalkErrorWriter : AbstractPacketWriter
{
    public required int StalkError { get; init; }
}