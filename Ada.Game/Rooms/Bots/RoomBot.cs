using System.Drawing;
using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Bots;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Pathfinding;
using Ada.Core.Enums.Miscellaneous;
using Ada.Game.Rooms.Unit;

namespace Ada.Game.Rooms.Bots;

public class RoomBot(
    IRoomLogic room,
    Point point,
    double pointZ,
    HDirection directionHead,
    HDirection direction,
    IRoomTileMapHelperService tileMapHelperService,
    PlayerBotDto playerBot,
    IRoomPathFinderHelperService pathFinderHelperService)
    : RoomUnitData(room,
            point,
            pointZ,
            directionHead,
            direction,
            tileMapHelperService,
            pathFinderHelperService),
        IRoomBot
{
    public required PlayerBotDto Bot { get; init; } = playerBot;

    public async Task RunPeriodicCheckAsync()
    {
        await ProcessGenericChecksAsync();
    }
}