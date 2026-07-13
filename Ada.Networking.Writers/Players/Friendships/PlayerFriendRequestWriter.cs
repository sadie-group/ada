using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players.Friendships;

[PacketId(ServerPacketId.PlayerFriendRequest)]
public class PlayerFriendRequestWriter : AbstractPacketWriter
{
    public required long Id { get; set; }
    public required string Username { get; set; }
    public required string FigureCode { get; set; }
}