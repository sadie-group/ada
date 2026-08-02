using System.Drawing;
using Ada.API.DTOs.Furniture;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Pathfinding;
using Ada.API.Interfaces.Game.Rooms.Unit;
using Ada.Core.Enums.Game.Rooms.Users;
using Ada.Core.Enums.Miscellaneous;
using Ada.Game.Rooms.Mapping;
using Ada.Game.Rooms.PathFinding;
using Ada.Game.Rooms.Unit;
using Moq;

namespace Ada.Tests.Game.Rooms.Unit;

[TestFixture]
public class RoomUnitDataWalkTests
{
    private class TestableUnit(
        IRoomLogic room,
        Point point,
        IRoomTileMapHelperService tileMapHelperService,
        IRoomPathFinderHelperService pathFinderHelperService)
        : RoomUnitData(room, point, 0, HDirection.North, HDirection.North, tileMapHelperService, pathFinderHelperService)
    {
        public bool NeedsPath => NeedsPathCalculated;
        public Task ProcessAsync() => ProcessGenericChecksAsync();
    }

    private static PlayerFurnitureItemPlacementDataDto FurnitureAt(
        int x,
        int y,
        bool canSit = false,
        bool canLay = false,
        double stackHeight = 1.0)
        => new()
        {
            PlayerFurnitureItem = new PlayerFurnitureItemDto
            {
                FurnitureItemId = 0,
                LimitedData = "",
                MetaData = "",
                FurnitureItem = new FurnitureItemDto
                {
                    Name = "",
                    AssetName = "",
                    InteractionType = null,
                    CanSit = canSit,
                    CanLay = canLay,
                    StackHeight = stackHeight
                }
            },
            PositionX = x,
            PositionY = y,
            Direction = HDirection.East
        };

    private static (TestableUnit Unit, RoomTileMap TileMap) CreateUnit(
        Point start,
        string heightmap = "000\n000\n000",
        List<PlayerFurnitureItemPlacementDataDto>? furniture = null,
        IRoomPathFinderHelperService? pathHelper = null)
    {
        furniture ??= [];
        var tileMap = new RoomTileMap(heightmap, furniture);
        var roomDto = new RoomDto
        {
            FurnitureItems = furniture
        };

        var room = new Mock<IRoomLogic>();
        room.SetupGet(x => x.Room).Returns(roomDto);
        room.SetupGet(x => x.TileMap).Returns(tileMap);
        room.SetupGet(x => x.PathFinder).Returns(new RoomPathFinder(tileMap.SizeY, tileMap.SizeX));

        var unit = new TestableUnit(room.Object, start, tileMap, pathHelper ?? new RoomPathFinderHelperService());
        tileMap.AddUnitToMap(start, unit);

        return (unit, tileMap);
    }

    private static Mock<IRoomPathFinderHelperService> MockPathHelper(params List<Point>[] paths)
    {
        var helper = new Mock<IRoomPathFinderHelperService>();
        var sequence = helper.SetupSequence(x =>
            x.BuildPathForWalk(It.IsAny<IRoomLogic>(), It.IsAny<Point>(), It.IsAny<Point>(), It.IsAny<List<Point>>()));

        foreach (var path in paths)
        {
            sequence = sequence.Returns(path);
        }

        helper.Setup(x => x.GetDirectionForNextStep(It.IsAny<Point>(), It.IsAny<Point>()))
            .Returns(HDirection.South);

        return helper;
    }

    [Test]
    public void CheckStatusForCurrentTile_WhileWalking_LeavesStatusesUntouched()
    {
        var (unit, _) = CreateUnit(new Point(1, 1), "000\n020\n000");
        unit.AddStatus(RoomUserStatus.Sit, "1");
        unit.IsWalking = true;
        unit.NeedsUpdate = false;

        unit.CheckStatusForCurrentTile();

        Assert.Multiple(() =>
        {
            Assert.That(unit.StatusMap.ContainsKey(RoomUserStatus.Sit), Is.True);
            Assert.That(unit.NeedsUpdate, Is.False);
        });
    }

    [Test]
    public void CheckStatusForCurrentTile_PlainItemOnTile_ClearsSitLayAndTakesItemHeight()
    {
        var table = FurnitureAt(1, 1, stackHeight: 0.5);
        var (unit, _) = CreateUnit(new Point(1, 1), furniture: [table]);
        unit.AddStatus(RoomUserStatus.Sit, "1");

        unit.CheckStatusForCurrentTile();

        Assert.Multiple(() =>
        {
            Assert.That(unit.StatusMap.ContainsKey(RoomUserStatus.Sit), Is.False);
            Assert.That(unit.StatusMap.ContainsKey(RoomUserStatus.Lay), Is.False);
            Assert.That(unit.PointZ, Is.EqualTo(0.5));
            Assert.That(unit.NeedsUpdate, Is.True);
        });
    }

    [Test]
    public async Task TryStartPendingWalkAsync_NoPendingRequest_ReturnsFalse()
    {
        var (unit, _) = CreateUnit(new Point(0, 0));

        Assert.That(await unit.TryStartPendingWalkAsync(), Is.False);
    }

    [Test]
    public async Task TryStartPendingWalkAsync_NoRouteToGoal_ReturnsFalse()
    {
        var helper = MockPathHelper([new Point(0, 0)]);
        var (unit, _) = CreateUnit(new Point(0, 0), pathHelper: helper.Object);
        unit.WalkToPoint(new Point(2, 2));

        var started = await unit.TryStartPendingWalkAsync();

        Assert.Multiple(() =>
        {
            Assert.That(started, Is.False);
            Assert.That(unit.IsWalking, Is.False);
            Assert.That(unit.NeedsPath, Is.False);
        });
    }

    [Test]
    public async Task TryStartPendingWalkAsync_ReachableGoal_StartsWalking()
    {
        var (unit, _) = CreateUnit(new Point(0, 0));
        unit.WalkToPoint(new Point(2, 2));

        var started = await unit.TryStartPendingWalkAsync();

        Assert.Multiple(() =>
        {
            Assert.That(started, Is.True);
            Assert.That(unit.IsWalking, Is.True);
            Assert.That(unit.StatusMap.ContainsKey(RoomUserStatus.Move), Is.True);
        });
    }

    [Test]
    public async Task ProcessGenericChecks_RetargetedToUnreachableGoal_StopsWithoutCallback()
    {
        var helper = MockPathHelper(
            [new Point(0, 0), new Point(0, 1), new Point(0, 2)],
            [new Point(0, 1)]);
        var (unit, _) = CreateUnit(new Point(0, 0), pathHelper: helper.Object);
        var reached = false;

        unit.WalkToPoint(new Point(0, 2));
        await unit.ProcessAsync();

        unit.WalkToPoint(new Point(2, 0), () => reached = true);
        await unit.ProcessAsync();

        Assert.Multiple(() =>
        {
            Assert.That(reached, Is.False);
            Assert.That(unit.IsWalking, Is.False);
            Assert.That(unit.StatusMap.ContainsKey(RoomUserStatus.Move), Is.False);
        });
    }

    [Test]
    public async Task ProcessGenericChecks_PartialPathExhausted_StopsAtLastReachedPoint()
    {
        var helper = MockPathHelper([new Point(0, 0), new Point(0, 1)]);
        var (unit, _) = CreateUnit(new Point(0, 0), pathHelper: helper.Object);
        var reached = false;

        unit.WalkToPoint(new Point(2, 2), () => reached = true);
        await unit.TryStartPendingWalkAsync();
        await unit.ProcessAsync();

        Assert.Multiple(() =>
        {
            Assert.That(unit.Point, Is.EqualTo(new Point(0, 1)));
            Assert.That(unit.IsWalking, Is.False);
            Assert.That(reached, Is.True);
        });
    }

    [Test]
    public async Task ProcessGenericChecks_NextStepOccupied_RequestsNewPath()
    {
        var helper = MockPathHelper([new Point(0, 0), new Point(1, 0), new Point(2, 0)]);
        var (unit, tileMap) = CreateUnit(new Point(0, 0), pathHelper: helper.Object);
        tileMap.AddUnitToMap(new Point(1, 0), Mock.Of<IRoomUnitData>());

        unit.WalkToPoint(new Point(2, 0));
        await unit.TryStartPendingWalkAsync();

        Assert.Multiple(() =>
        {
            Assert.That(unit.IsWalking, Is.True);
            Assert.That(unit.NextPoint, Is.Null);
            Assert.That(unit.NeedsPath, Is.True);
            Assert.That(unit.StatusMap.ContainsKey(RoomUserStatus.Move), Is.False);
        });
    }
}
