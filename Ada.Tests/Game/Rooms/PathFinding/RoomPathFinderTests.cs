using System.Drawing;
using Ada.Game.Rooms.PathFinding;
using Ada.Game.Rooms.PathFinding.ToGo;
using Ada.Game.Rooms.PathFinding.ToGo.Options;

namespace Ada.Tests.Game.Rooms.PathFinding;

[TestFixture]
public class RoomPathFinderTests
{
    private static WorldGrid OpenWorld(int height, int width)
    {
        var world = new short[height, width];
        for (var row = 0; row < height; row++)
        {
            for (var column = 0; column < width; column++)
            {
                world[row, column] = 1;
            }
        }

        return new WorldGrid(world);
    }

    private static void AssertStepsAreAdjacent(List<Point> path)
    {
        for (var i = 1; i < path.Count; i++)
        {
            var dx = Math.Abs(path[i].X - path[i - 1].X);
            var dy = Math.Abs(path[i].Y - path[i - 1].Y);
            Assert.That(Math.Max(dx, dy), Is.EqualTo(1), $"step {i - 1}->{i} is not adjacent");
        }
    }

    [Test]
    public void FindPath_OpenGrid_ReturnsPathFromStartToEnd()
    {
        var finder = new RoomPathFinder(5, 5);

        var path = finder.FindPath(new Point(0, 0), new Point(4, 4), OpenWorld(5, 5)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(path, Is.Not.Empty);
            Assert.That(path.First(), Is.EqualTo(new Point(0, 0)));
            Assert.That(path.Last(), Is.EqualTo(new Point(4, 4)));
        });
        AssertStepsAreAdjacent(path);
    }

    [Test]
    public void FindPath_StartEqualsEnd_ReturnsSinglePoint()
    {
        var finder = new RoomPathFinder(3, 3);

        var path = finder.FindPath(new Point(1, 1), new Point(1, 1), OpenWorld(3, 3)).ToList();

        Assert.That(path, Is.EqualTo(new List<Point> { new(1, 1) }));
    }

    [Test]
    public void FindPath_EnclosedGoal_ReturnsEmpty()
    {
        var world = OpenWorld(5, 5);
        world[3, 3] = 0;
        world[3, 4] = 0;
        world[4, 3] = 0;

        var finder = new RoomPathFinder(5, 5);
        var path = finder.FindPath(new Point(0, 0), new Point(4, 4), world);

        Assert.That(path, Is.Empty);
    }

    [Test]
    public void FindPath_WallWithGap_RoutesThroughGapAvoidingClosedTiles()
    {
        var world = OpenWorld(5, 5);
        for (var row = 0; row < 4; row++)
        {
            world[row, 2] = 0;
        }

        var finder = new RoomPathFinder(5, 5);
        var path = finder.FindPath(new Point(0, 0), new Point(4, 0), world).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(path, Is.Not.Empty);
            Assert.That(path.Last(), Is.EqualTo(new Point(4, 0)));
            Assert.That(path.Where(p => p.X == 2), Has.All.Matches<Point>(p => p.Y == 4), "must cross the wall column at the gap row");
        });
        AssertStepsAreAdjacent(path);
    }

    [Test]
    public void FindPath_WithoutDiagonals_TakesManhattanPath()
    {
        var finder = new RoomPathFinder(4, 4, new PathFinderOptions { UseDiagonals = false });

        var path = finder.FindPath(new Point(0, 0), new Point(3, 3), OpenWorld(4, 4)).ToList();

        Assert.That(path, Has.Count.EqualTo(7), "3 + 3 steps plus the start tile");
        for (var i = 1; i < path.Count; i++)
        {
            var dx = Math.Abs(path[i].X - path[i - 1].X);
            var dy = Math.Abs(path[i].Y - path[i - 1].Y);
            Assert.That(dx + dy, Is.EqualTo(1), "diagonal step taken despite UseDiagonals = false");
        }
    }

    [Test]
    public void FindPath_WithDiagonals_IsShorterThanManhattanPath()
    {
        var finder = new RoomPathFinder(4, 4);

        var path = finder.FindPath(new Point(0, 0), new Point(3, 3), OpenWorld(4, 4)).ToList();

        Assert.That(path, Has.Count.EqualTo(4), "pure diagonal walk plus the start tile");
    }

    [Test]
    public void FindPath_PunishChangeDirection_StillFindsValidPath()
    {
        var finder = new RoomPathFinder(5, 5, new PathFinderOptions { PunishChangeDirection = true });

        var path = finder.FindPath(new Point(0, 0), new Point(4, 2), OpenWorld(5, 5)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(path.First(), Is.EqualTo(new Point(0, 0)));
            Assert.That(path.Last(), Is.EqualTo(new Point(4, 2)));
        });
        AssertStepsAreAdjacent(path);
    }

    [Test]
    public void FindPath_PositionOverload_ReturnsSamePathAsPointOverload()
    {
        var finder = new RoomPathFinder(4, 4);

        var positions = finder.FindPath(new Position(0, 0), new Position(3, 3), OpenWorld(4, 4)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(positions, Has.Count.EqualTo(4));
            Assert.That(positions.First().Row, Is.EqualTo(0));
            Assert.That(positions.Last().Row, Is.EqualTo(3));
            Assert.That(positions.Last().Column, Is.EqualTo(3));
        });
    }

    [Test]
    public void FindPath_ReusedFinder_ProducesFreshPath()
    {
        var finder = new RoomPathFinder(5, 5);
        var world = OpenWorld(5, 5);

        var first = finder.FindPath(new Point(0, 0), new Point(4, 4), world).ToList();
        var second = finder.FindPath(new Point(4, 4), new Point(0, 0), world).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(first.Last(), Is.EqualTo(new Point(4, 4)));
            Assert.That(second.First(), Is.EqualTo(new Point(4, 4)));
            Assert.That(second.Last(), Is.EqualTo(new Point(0, 0)));
        });
    }
}
