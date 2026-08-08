using System.Drawing;
using Ada.API.Interfaces.Game.Rooms.Pathfinding;
using Ada.API.Interfaces.Game.Rooms.Pathfinding.ToGo;
using Ada.API.Interfaces.Game.Rooms.Pathfinding.ToGo.Heuristics;
using Ada.Game.Rooms.PathFinding.ToGo;
using Ada.Game.Rooms.PathFinding.ToGo.Collections.PathFinder;
using Ada.Game.Rooms.PathFinding.ToGo.Heuristics;
using Ada.Game.Rooms.PathFinding.ToGo.Options;

namespace Ada.Game.Rooms.PathFinding;

public class RoomPathFinder : IRoomPathFinder
{
    private const int _closed = 0;
    private const int _stepCost = 1;
    private readonly PathFinderOptions _opts;
    private readonly ICalculateHeuristic _heuristic;
    private readonly PathFinderGraph _graph;
    private readonly Position[] _backtrackBuf;
    private readonly Lock _searchLock = new();

    public RoomPathFinder(int height, int width, PathFinderOptions? opts = null)
    {
        _opts = opts ?? new PathFinderOptions();
        _heuristic = HeuristicFactory.Create(_opts.HeuristicFormula);
        _graph = new PathFinderGraph(height, width, _opts.UseDiagonals);
        _backtrackBuf = new Position[height * width];
    }

    public List<Point> FindPath(Point start, Point end, IWorldGrid world)
    {
        lock (_searchLock)
        {
            var count = InternalFindPath(new Position(start.Y, start.X), new Position(end.Y, end.X), world);

            var path = new List<Point>(count);

            for (var i = 0; i < count; i++)
            {
                var p = _backtrackBuf[i];
                path.Add(new Point(p.Column, p.Row));
            }

            return path;
        }
    }

    public List<IPosition> FindPath(IPosition start, IPosition end, IWorldGrid world)
    {
        lock (_searchLock)
        {
            var count = InternalFindPath(new Position(start.Row, start.Column), new Position(end.Row, end.Column), world);

            var path = new List<IPosition>(count);

            for (var i = 0; i < count; i++)
            {
                path.Add(_backtrackBuf[i]);
            }

            return path;
        }
    }

    private int InternalFindPath(Position start, Position end, IWorldGrid world)
    {
        _graph.Reset();

        var startNode = new PathFinderNode(start, 0, 0, start);
        _graph.OpenNode(startNode);

        var visited = 0;

        while (_graph.HasOpenNodes)
        {
            var q = _graph.GetOpenNodeWithSmallestF();

            if (q.Position.Equals(end))
            {
                return Backtrack(q);
            }

            if (visited++ > _opts.SearchLimit)
            {
                return 0;
            }

            foreach (var s in _graph.GetSuccessors(q))
            {
                if (world[s.Position.Row, s.Position.Column] == _closed)
                {
                    continue;
                }
                var g = q.G + _stepCost;
                if (_opts.PunishChangeDirection)
                {
                    g += CalculateModifier(q, s, end);
                }

                var n = new PathFinderNode(s.Position, g, _heuristic.Calculate(s.Position, end), q.Position);

                if (!_graph.WasVisited(s.Position) || n.F < s.F)
                {
                    _graph.OpenNode(n);
                }
            }
        }

        return 0;
    }

    private int Backtrack(PathFinderNode endNode)
    {
        var count = 0;
        var current = endNode;
        var guard = 0;

        while (!current.Position.Equals(current.ParentNodePosition))
        {
            if (guard++ > 50000)
            {
                return 0;
            }
            _backtrackBuf[count++] = current.Position;
            current = _graph.GetParent(current);
        }

        _backtrackBuf[count++] = current.Position;
        var left = 0;
        var right = count - 1;

        while (left < right)
        {
            var tmp = _backtrackBuf[left];

            _backtrackBuf[left] = _backtrackBuf[right];
            _backtrackBuf[right] = tmp;

            left++;
            right--;
        }

        return count;
    }

    private static int CalculateModifier(PathFinderNode q, PathFinderNode s, Position end)
    {
        if (q.Position == q.ParentNodePosition)
        {
            return 0;
        }

        var p = Math.Abs(s.Position.Row - end.Row) + Math.Abs(s.Position.Column - end.Column);

        if (s.Position.Row != q.Position.Row)
        {
            if (q.Position.Row == q.ParentNodePosition.Row)
            {
                return p;
            }
        }

        if (s.Position.Column != q.Position.Column)
        {
            if (q.Position.Column == q.ParentNodePosition.Column)
            {
                return p;
            }
        }

        return 0;
    }
}
