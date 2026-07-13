using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players;

[PacketId(ServerPacketId.PlayerIgnoredUsers)]
public class PlayerIgnoredUsersWriter : AbstractPacketWriter
{
    public required List<string> IgnoredUsernames { get; init; }
}