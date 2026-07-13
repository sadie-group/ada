using System.Drawing;
using Ada.API.Interfaces.Game.Rooms.Pathfinding.ToGo;

namespace Ada.API.Interfaces.Game.Rooms.Pathfinding;

public interface IRoomPathFinder
{
    IEnumerable<Point> FindPath(Point start, Point end, IWorldGrid world);
}