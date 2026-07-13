using Ada.API.Interfaces.Game.Players.Friendships;
using Ada.Core.Enums.Game.Players;

namespace Ada.Networking.Events.Dtos;

public class PlayerFriendshipUpdate : IPlayerFriendshipUpdate
{
    public required int Type { get; init; }
    public required IFriendData? Friend { get; init; }
    public required bool FriendOnline { get; init; }
    public required bool FriendInRoom { get; init; }
    public required PlayerRelationshipType Relation { get; init; }
}