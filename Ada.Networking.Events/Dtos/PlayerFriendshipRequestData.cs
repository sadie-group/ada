using Ada.API.Interfaces.Game.Players.Friendships;

namespace Ada.Networking.Events.Dtos;

public class PlayerFriendshipRequestData : IPlayerFriendshipRequestData
{
    public long Id { get; init; }
    public required string Username { get; init; }
    public required string FigureCode { get; init; }
}