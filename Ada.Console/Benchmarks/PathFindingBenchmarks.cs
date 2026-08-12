using System.Drawing;
using Ada.API.DTOs.Players.Furniture;
using Ada.Game.Rooms.Mapping;
using Ada.Game.Rooms.PathFinding;
using Ada.Game.Rooms.PathFinding.ToGo;
using Ada.Game.Rooms.PathFinding.ToGo.Options;
using BenchmarkDotNet.Attributes;

namespace Ada.Console.Benchmarks;

[MemoryDiagnoser]
public class PathFindingBenchmarks
{
    private RoomPathFinder _pathFinder = null!;
    private WorldGrid _worldGrid = null!;
    private Point _start;
    private Point _end;

    [Params(20, 50)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var heightmap = string.Join('\n', Enumerable.Repeat(new string('0', Size), Size));

        var tileMap = new RoomTileMap(heightmap, new List<PlayerFurnitureItemPlacementDataDto>());

        _worldGrid = new WorldGrid(tileMap.SizeY, tileMap.SizeX);

        for (var y = 0; y < tileMap.SizeY; y++)
        {
            for (var x = 0; x < tileMap.SizeX; x++)
            {
                _worldGrid[y, x] = tileMap.Map[y, x];
            }
        }

        _pathFinder = new RoomPathFinder(tileMap.SizeY, tileMap.SizeX, new PathFinderOptions
        {
            UseDiagonals = true
        });

        _start = new Point(0, 0);
        _end = new Point(Size - 1, Size - 1);
    }

    [Benchmark(Description = "Corner-to-corner A* on an empty square room")]
    public List<Point> CornerToCorner() => _pathFinder.FindPath(_start, _end, _worldGrid);

    [Benchmark(Description = "Unreachable target (worst case, exhausts the open set)")]
    public List<Point> Unreachable() => _pathFinder.FindPath(_start, new Point(Size + 50, Size + 50), _worldGrid);
}
