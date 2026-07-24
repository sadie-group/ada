using System.Drawing;
using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Pathfinding;
using Ada.API.Interfaces.Game.Rooms.Pets;
using Ada.Core.Enums.Miscellaneous;
using Ada.Game.Rooms.Unit;

namespace Ada.Game.Rooms.Pets;

public class RoomPet(
    IRoomLogic room,
    Point point,
    double pointZ,
    IRoomTileMapHelperService tileMapHelperService,
    PlayerPetDto playerPet,
    IRoomPathFinderHelperService pathFinderHelperService)
    : RoomUnitData(room,
            point,
            pointZ,
            HDirection.South,
            HDirection.South,
            tileMapHelperService,
            pathFinderHelperService),
        IRoomPet
{
    public required PlayerPetDto Pet { get; init; } = playerPet;
    public long? RiderId { get; set; }

    public async Task RunPeriodicCheckAsync()
    {
        await ProcessGenericChecksAsync();
    }
}
