using Ada.API.Interfaces.Game.Players.Friendships;
using Ada.Core.Enums.Game.Players;

namespace Ada.Networking.Events.Dtos;

public class FriendData : PlayerFriendshipRequestData, IFriendData
{
    public required string Motto { get; init; }
    public required PlayerAvatarGender Gender { get; init; }
}