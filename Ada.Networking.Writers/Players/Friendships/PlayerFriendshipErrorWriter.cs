using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players.Friendships;

[PacketId(ServerPacketId.PlayerFriendshipError)]
public class PlayerFriendshipErrorWriter : AbstractPacketWriter
{
    public required int ClientMessageId { get; init; }
    public required int Error { get; init; }
}