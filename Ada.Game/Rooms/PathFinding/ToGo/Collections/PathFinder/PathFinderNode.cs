using Ada.Game.Rooms.PathFinding.ToGo;

namespace Ada.Game.Rooms.PathFinding.ToGo.Collections.PathFinder;

public readonly struct PathFinderNode(Position p, int g, int h, Position parent)
{
    public Position Position { get; } = p;
    public int G { get; } = g;
    public int H { get; } = h;
    public int F { get; } = g + h;
    public Position ParentNodePosition { get; } = parent;
}