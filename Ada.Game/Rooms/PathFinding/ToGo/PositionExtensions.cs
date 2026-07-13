using System.Drawing;

namespace Ada.Game.Rooms.PathFinding.ToGo;

public static class PositionExtensions
{
    public static Position ToPosition(this Point point)
    {
        return new Position(point.Y, point.X);
    }
}