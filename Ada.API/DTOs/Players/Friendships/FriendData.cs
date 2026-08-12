using Ada.API.Interfaces.Game.Players.Friendships;
using Ada.Core.Enums.Game.Players;

namespace Ada.API.DTOs.Players.Friendships;

public class FriendData : PlayerFriendshipRequestData, IFriendData
{
    public required string Motto { get; init; }
    public required PlayerAvatarGender Gender { get; init; }
}