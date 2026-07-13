using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Rooms.Bots;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Pathfinding;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Networking;

namespace Ada.API.Interfaces.Game.Rooms;

public interface IRoomLogic : IAsyncDisposable
{
    RoomDto Room { get; }
    IRoomTileMap TileMap { get; }
    IRoomPathFinder PathFinder { get; }
    IRoomUserRepository UserRepository { get; }
    IRoomBotRepository BotRepository { get; }
    Task BroadcastDataAsync(AbstractPacketWriter writer, IReadOnlyCollection<long>? excludedIds = null);
    ValueTask DisposeAsync();
}