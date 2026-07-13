using Ada.Core.Shared.Attributes;

namespace Ada.API.Interfaces.Game.Players.Friendships;

public interface IPlayerFriendshipRequestData
{
    [PacketData] long Id { get; init; }
    [PacketData] string Username { get; init; }
    [PacketData] string FigureCode { get; init; }
}