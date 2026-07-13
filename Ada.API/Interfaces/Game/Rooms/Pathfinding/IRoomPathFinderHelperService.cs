using System.Drawing;
using Ada.Core.Enums.Miscellaneous;

namespace Ada.API.Interfaces.Game.Rooms.Pathfinding;

public interface IRoomPathFinderHelperService
{
    HDirection GetDirectionForNextStep(Point current, Point next);

    List<Point> BuildPathForWalk(IRoomLogic room,
        Point start,
        Point end,
        List<Point> overridePoints);
}