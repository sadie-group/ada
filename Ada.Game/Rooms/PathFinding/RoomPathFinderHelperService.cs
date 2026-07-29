using System.Drawing;
using System.Runtime.CompilerServices;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Pathfinding;
using Ada.Core.Enums.Miscellaneous;
using Ada.Game.Rooms.PathFinding.ToGo;

namespace Ada.Game.Rooms.PathFinding;

public class RoomPathFinderHelperService : IRoomPathFinderHelperService
{
    // One scratch grid per room, refilled per path request. Path requests for a
    // room only run from that room's tick, so the grid is never used concurrently.
    private static readonly ConditionalWeakTable<IRoomTileMap, WorldGrid> ScratchGrids = new();

    public HDirection GetDirectionForNextStep(Point current, Point next)
    {
        var rotation = HDirection.North;

        if (current.X > next.X && current.Y > next.Y)
        {
            rotation = HDirection.NorthWest;
        }
        else if (current.X < next.X && current.Y < next.Y)
        {
            rotation = HDirection.SouthEast;
        }
        else if (current.X > next.X && current.Y < next.Y)
        {
            rotation = HDirection.SouthWest;
        }
        else if (current.X < next.X && current.Y > next.Y)
        {
            rotation = HDirection.NorthEast;
        }
        else if (current.X > next.X)
        {
            rotation = HDirection.West;
        }
        else if (current.X < next.X)
        {
            rotation = HDirection.East;
        }
        else if (current.Y < next.Y)
        {
            rotation = HDirection.South;
        }
        else if (current.Y > next.Y)
        {
            rotation = HDirection.North;
        }

        return rotation;
    }
    
    public List<Point> BuildPathForWalk(IRoomLogic room,
        Point start,
        Point end,
        List<Point> overridePoints)
    {
        var tileMap = room.TileMap;
        var worldGrid = ScratchGrids.GetValue(tileMap, static map => new WorldGrid(map.SizeY, map.SizeX));

        FillWorldGrid(worldGrid, tileMap, end, overridePoints);

        return room
            .PathFinder
            .FindPath(start, end, worldGrid)
            .ToList();
    }

    private static void FillWorldGrid(WorldGrid grid,
        IRoomTileMap map,
        Point goalPoint,
        List<Point> overridePoints)
    {
        var tileStates = map.Map;

        for (var y = 0; y < map.SizeY; y++)
        {
            for (var x = 0; x < map.SizeX; x++)
            {
                var state = tileStates[y, x];

                // Sit and lay tiles are only walkable when they are the goal
                grid[y, x] = (state == 2 || state == 3) && (goalPoint.X != x || goalPoint.Y != y)
                    ? (short) 0
                    : state;
            }
        }

        foreach (var entry in map.UnitMap)
        {
            var point = entry.Key;

            if (entry.Value.Count == 0 ||
                (uint) point.X >= (uint) map.SizeX ||
                (uint) point.Y >= (uint) map.SizeY)
            {
                continue;
            }

            grid[point.Y, point.X] = 0;
        }

        foreach (var point in overridePoints)
        {
            if ((uint) point.X >= (uint) map.SizeX ||
                (uint) point.Y >= (uint) map.SizeY)
            {
                continue;
            }

            grid[point.Y, point.X] = 1;
        }
    }
}