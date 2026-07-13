using System.Drawing;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Pathfinding;
using Ada.Core.Enums.Miscellaneous;
using Ada.Game.Rooms.PathFinding.ToGo;

namespace Ada.Game.Rooms.PathFinding;

public class RoomPathFinderHelperService : IRoomPathFinderHelperService
{
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
        var worldArray = tileMap.GetWorldArrayFromTileMap(tileMap, end, overridePoints);
        var worldGrid = new WorldGrid(worldArray);

        return room
            .PathFinder
            .FindPath(start, end, worldGrid)
            .ToList();
    }
}