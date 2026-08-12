using System.Drawing;
using Ada.Game.Rooms.PathFinding.ToGo;

namespace Ada.Tests.Game.Rooms.PathFinding;

[TestFixture]
public class WorldGridTests
{
    private static WorldGrid CreateGrid()
    {
        var world = new short[3, 4];
        world[1, 2] = 7;
        return new WorldGrid(world);
    }

    [Test]
    public void Constructor_SetsDimensionsFromArray()
    {
        var grid = CreateGrid();

        Assert.Multiple(() =>
        {
            Assert.That(grid.Height, Is.EqualTo(3));
            Assert.That(grid.Width, Is.EqualTo(4));
        });
    }

    [Test]
    public void Indexers_PointPositionAndRowColumn_AddressSameCell()
    {
        var grid = CreateGrid();

        Assert.Multiple(() =>
        {
            Assert.That(grid[1, 2], Is.EqualTo(7));
            Assert.That(grid[new Position(1, 2)], Is.EqualTo(7));
            Assert.That(grid[new Point(2, 1)], Is.EqualTo(7), "Point.X = column, Point.Y = row");
        });
    }

    [Test]
    public void GetSuccessorPositions_CentreTile_ReturnsFourOrEightNeighbours()
    {
        var grid = CreateGrid();
        var centre = new Position(1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(grid.GetSuccessorPositions(centre).Count(), Is.EqualTo(4));
            Assert.That(grid.GetSuccessorPositions(centre, true).Count(), Is.EqualTo(8));
        });
    }

    [Test]
    public void GetSuccessorPositions_CornerTile_StaysInBounds()
    {
        var grid = CreateGrid();

        var successors = grid.GetSuccessorPositions(new Position(0, 0), true).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(successors, Has.Count.EqualTo(3));
            Assert.That(successors, Has.All.Matches<Ada.API.Interfaces.Game.Rooms.Pathfinding.ToGo.IPosition>(
                p => p.Row is >= 0 and < 3 && p.Column is >= 0 and < 4));
        });
    }
}
