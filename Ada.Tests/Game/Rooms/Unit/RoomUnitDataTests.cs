using System.Drawing;
using Ada.API.DTOs.Furniture;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Pathfinding;
using Ada.Core.Enums.Game.Rooms.Users;
using Ada.Core.Enums.Miscellaneous;
using Ada.Game.Rooms.Mapping;
using Ada.Game.Rooms.PathFinding;
using Ada.Game.Rooms.Unit;
using Moq;

namespace Ada.Tests.Game.Rooms.Unit;

[TestFixture]
public class RoomUnitDataTests
{
    private class TestableRoomUnitData(
        IRoomLogic room,
        Point point,
        IRoomTileMapHelperService tileMapHelperService,
        IRoomPathFinderHelperService pathFinderHelperService)
        : RoomUnitData(room, point, 0, HDirection.North, HDirection.North, tileMapHelperService, pathFinderHelperService)
    {
        public bool NeedsPath => NeedsPathCalculated;
        public Point Goal => PathGoal;
        public Task ProcessAsync() => ProcessGenericChecksAsync();
    }

    private static PlayerFurnitureItemPlacementDataDto FurnitureAt(
        int x,
        int y,
        bool canSit = false,
        bool canLay = false,
        double stackHeight = 1.0,
        HDirection direction = HDirection.East)
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
                    StackHeight = stackHeight,
                },
            },
            PositionX = x,
            PositionY = y,
            Direction = direction,
        };

    private static (TestableRoomUnitData unit, RoomTileMap tileMap) CreateUnit(
        Point start,
        string heightmap = "000\n000\n000",
        List<PlayerFurnitureItemPlacementDataDto>? furniture = null,
        bool canUsersOverlap = false)
    {
        furniture ??= [];
        var tileMap = new RoomTileMap(heightmap, furniture);
        var roomDto = new RoomDto
        {
            FurnitureItems = [..furniture],
            Settings = new RoomSettingsDto { CanUsersOverlap = canUsersOverlap },
        };

        var room = new Mock<IRoomLogic>();
        room.SetupGet(x => x.Room).Returns(roomDto);
        room.SetupGet(x => x.TileMap).Returns(tileMap);
        room.SetupGet(x => x.PathFinder).Returns(new RoomPathFinder(tileMap.SizeY, tileMap.SizeX));

        var unit = new TestableRoomUnitData(room.Object, start, tileMap, new RoomPathFinderHelperService());
        tileMap.AddUnitToMap(start, unit);

        return (unit, tileMap);
    }

    [Test]
    public void AddStatus_SetsValueAndFlagsUpdate()
    {
        var (unit, _) = CreateUnit(new Point(0, 0));
        unit.NeedsUpdate = false;

        unit.AddStatus(RoomUserStatus.Dance, "1");

        Assert.Multiple(() =>
        {
            Assert.That(unit.StatusMap[RoomUserStatus.Dance], Is.EqualTo("1"));
            Assert.That(unit.NeedsUpdate, Is.True);
        });
    }

    [Test]
    public void RemoveStatuses_RemovesGivenKeysAndFlagsUpdate()
    {
        var (unit, _) = CreateUnit(new Point(0, 0));
        unit.AddStatus(RoomUserStatus.Dance, "1");
        unit.AddStatus(RoomUserStatus.Sign, "5");
        unit.NeedsUpdate = false;

        unit.RemoveStatuses(RoomUserStatus.Dance, RoomUserStatus.Sign);

        Assert.Multiple(() =>
        {
            Assert.That(unit.StatusMap, Is.Empty);
            Assert.That(unit.NeedsUpdate, Is.True);
        });
    }

    [Test]
    public void WalkToPoint_FreeTile_SetsGoalAndRequestsPath()
    {
        var (unit, _) = CreateUnit(new Point(0, 0));

        unit.WalkToPoint(new Point(2, 2));

        Assert.Multiple(() =>
        {
            Assert.That(unit.NeedsPath, Is.True);
            Assert.That(unit.Goal, Is.EqualTo(new Point(2, 2)));
        });
    }

    [Test]
    public void WalkToPoint_OccupiedTileWithoutOverlap_Ignored()
    {
        var (unit, tileMap) = CreateUnit(new Point(0, 0));
        var (other, _) = CreateUnit(new Point(2, 2));
        tileMap.AddUnitToMap(new Point(2, 2), other);

        unit.WalkToPoint(new Point(2, 2));

        Assert.That(unit.NeedsPath, Is.False);
    }

    [Test]
    public void WalkToPoint_OccupiedTileWithOverlapAllowed_RequestsPath()
    {
        var (unit, tileMap) = CreateUnit(new Point(0, 0), canUsersOverlap: true);
        var (other, _) = CreateUnit(new Point(2, 2));
        tileMap.AddUnitToMap(new Point(2, 2), other);

        unit.WalkToPoint(new Point(2, 2));

        Assert.That(unit.NeedsPath, Is.True);
    }

    [Test]
    public void CheckStatusForCurrentTile_EmptyTile_TakesHeightFromZMapAndClearsSitLay()
    {
        var (unit, _) = CreateUnit(new Point(1, 1), "000\n020\n000");
        unit.AddStatus(RoomUserStatus.Sit, "1");

        unit.CheckStatusForCurrentTile();

        Assert.Multiple(() =>
        {
            Assert.That(unit.PointZ, Is.EqualTo(2));
            Assert.That(unit.StatusMap.ContainsKey(RoomUserStatus.Sit), Is.False);
        });
    }

    [Test]
    public void CheckStatusForCurrentTile_SeatOnTile_AddsSitStatusAndTurnsToSeat()
    {
        var seat = FurnitureAt(1, 1, canSit: true, stackHeight: 1.0, direction: HDirection.South);
        var (unit, _) = CreateUnit(new Point(1, 1), furniture: [seat]);

        unit.CheckStatusForCurrentTile();

        Assert.Multiple(() =>
        {
            Assert.That(unit.StatusMap[RoomUserStatus.Sit], Is.EqualTo("1"));
            Assert.That(unit.Direction, Is.EqualTo(HDirection.South));
            Assert.That(unit.DirectionHead, Is.EqualTo(HDirection.South));
        });
    }

    [Test]
    public void CheckStatusForCurrentTile_BedOnTile_AddsLayStatus()
    {
        var bed = FurnitureAt(1, 1, canLay: true, stackHeight: 0.5);
        var (unit, _) = CreateUnit(new Point(1, 1), furniture: [bed]);

        unit.CheckStatusForCurrentTile();

        Assert.That(unit.StatusMap[RoomUserStatus.Lay], Is.EqualTo("0.6"));
    }

    [Test]
    public async Task ProcessGenericChecksAsync_WalksToGoalAndInvokesCallback()
    {
        var (unit, tileMap) = CreateUnit(new Point(0, 0));
        var reachedGoal = false;
        unit.WalkToPoint(new Point(2, 2), () => reachedGoal = true);

        for (var tick = 0; tick < 10 && !reachedGoal; tick++)
        {
            await unit.ProcessAsync();
        }

        Assert.Multiple(() =>
        {
            Assert.That(reachedGoal, Is.True, "unit never reached its goal");
            Assert.That(unit.Point, Is.EqualTo(new Point(2, 2)));
            Assert.That(unit.IsWalking, Is.False);
            Assert.That(unit.StatusMap.ContainsKey(RoomUserStatus.Move), Is.False);
            Assert.That(tileMap.UnitMap[new Point(2, 2)], Does.Contain(unit));
        });
    }

    [Test]
    public async Task ProcessGenericChecksAsync_WhileMoving_PublishesMoveStatus()
    {
        var (unit, _) = CreateUnit(new Point(0, 0));
        unit.WalkToPoint(new Point(2, 2));

        await unit.ProcessAsync();

        Assert.Multiple(() =>
        {
            Assert.That(unit.IsWalking, Is.True);
            Assert.That(unit.StatusMap.ContainsKey(RoomUserStatus.Move), Is.True);
            Assert.That(unit.NextPoint, Is.Not.Null);
        });
    }
}
