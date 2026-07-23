using System.Drawing;
using Ada.API.Interfaces.Game.Rooms;
using Ada.Core.Enums.Miscellaneous;
using Ada.Game.Rooms.Mapping;
using Ada.Game.Rooms.PathFinding;
using Moq;

namespace Ada.Tests.Game.Rooms.PathFinding;

[TestFixture]
public class RoomPathFinderHelperServiceTests
{
    private readonly RoomPathFinderHelperService _service = new();

    [TestCase(1, 1, 0, 0, HDirection.NorthWest)]
    [TestCase(1, 1, 2, 2, HDirection.SouthEast)]
    [TestCase(1, 1, 0, 2, HDirection.SouthWest)]
    [TestCase(1, 1, 2, 0, HDirection.NorthEast)]
    [TestCase(1, 1, 0, 1, HDirection.West)]
    [TestCase(1, 1, 2, 1, HDirection.East)]
    [TestCase(1, 1, 1, 2, HDirection.South)]
    [TestCase(1, 1, 1, 0, HDirection.North)]
    [TestCase(1, 1, 1, 1, HDirection.North)]
    public void GetDirectionForNextStep_ReturnsExpectedDirection(int currentX, int currentY, int nextX, int nextY, HDirection expected)
    {
        var direction = _service.GetDirectionForNextStep(new Point(currentX, currentY), new Point(nextX, nextY));

        Assert.That(direction, Is.EqualTo(expected));
    }

    [Test]
    public void BuildPathForWalk_OpenRoom_ReturnsPathEndingAtGoal()
    {
        var tileMap = new RoomTileMap("000\n000\n000", []);
        var room = new Mock<IRoomLogic>();
        room.SetupGet(x => x.TileMap).Returns(tileMap);
        room.SetupGet(x => x.PathFinder).Returns(new RoomPathFinder(tileMap.SizeY, tileMap.SizeX));

        var path = _service.BuildPathForWalk(room.Object, new Point(0, 0), new Point(2, 2), []);

        Assert.Multiple(() =>
        {
            Assert.That(path, Is.Not.Empty);
            Assert.That(path.First(), Is.EqualTo(new Point(0, 0)));
            Assert.That(path.Last(), Is.EqualTo(new Point(2, 2)));
        });
    }
}
