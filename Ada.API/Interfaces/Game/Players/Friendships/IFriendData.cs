using Ada.Core.Enums.Game.Players;

namespace Ada.API.Interfaces.Game.Players.Friendships;

public interface IFriendData
{
    string Motto { get; init; }
    PlayerAvatarGender Gender { get; init; }
    long Id { get; init; }
    string Username { get; init; }
    string FigureCode { get; init; }
}