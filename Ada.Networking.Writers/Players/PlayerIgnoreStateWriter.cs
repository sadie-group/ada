using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players;

[PacketId(ServerPacketId.PlayerIgnoreState)]
public class PlayerIgnoreStateWriter : AbstractPacketWriter
{
    public required int State { get; init; }
    public required string Username { get; init; }
}