using System.Drawing;
using Ada.API.DTOs.Furniture;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Unit;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Enums.Miscellaneous;
using Ada.Game.Rooms.Mapping;
using Moq;

namespace Ada.Tests.Game.Rooms.Mapping;

[TestFixture]
public class RoomTileMapHelperServiceExtendedTests
{
    private IRoomTileMapHelperService _service;

    [SetUp]
    public void SetUp()
    {
        _service = new RoomTileMapHelperService();
    }

    private static PlayerFurnitureItemPlacementDataDto MakeItem(
        int x,
        int y,
        HDirection direction = HDirection.North,
        FurnitureItemType type = FurnitureItemType.Floor,
        bool canStack = false,
        bool canWalk = false,
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
                    Type = type,
                    CanStack = canStack,
                    CanWalk = canWalk,
                    StackHeight = stackHeight
                }
            },
            PositionX = x,
            PositionY = y,
            Direction = direction
        };

    [Test]
    public void GetOppositeDirection_UnknownDirection_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.GetOppositeDirection((HDirection)99));
    }

    [TestCase(HDirection.East)]
    [TestCase(HDirection.West)]
    public void GetPointsForPlacement_EastWest_SwapsWidthAndLength(HDirection direction)
    {
        var points = _service.GetPointsForPlacement(1, 2, 2, 3, direction);

        Assert.Multiple(() =>
        {
            Assert.That(points, Has.Count.EqualTo(6));
            Assert.That(points, Does.Contain(new Point(1, 2)));
            Assert.That(points, Does.Contain(new Point(3, 3)));
            Assert.That(points, Does.Not.Contain(new Point(1, 4)));
        });
    }

    [Test]
    public void GetItemsForPosition_DiagonalDirection_HasNoFootprint()
    {
        var items = new List<PlayerFurnitureItemPlacementDataDto>
        {
            MakeItem(1, 1, HDirection.NorthEast)
        };

        Assert.That(_service.GetItemsForPosition(1, 1, items), Is.Empty);
    }

    [Test]
    public void GetItemsForPosition_WallItem_Ignored()
    {
        var items = new List<PlayerFurnitureItemPlacementDataDto>
        {
            MakeItem(1, 1, type: FurnitureItemType.Wall)
        };

        Assert.That(_service.GetItemsForPosition(1, 1, items), Is.Empty);
    }

    [Test]
    public void GetItemsForPosition_CollectionMutated_RebuildsIndex()
    {
        var items = new List<PlayerFurnitureItemPlacementDataDto>
        {
            MakeItem(0, 0)
        };

        Assert.That(_service.GetItemsForPosition(0, 0, items), Has.Count.EqualTo(1));

        items.Add(MakeItem(2, 2));

        Assert.Multiple(() =>
        {
            Assert.That(_service.GetItemsForPosition(2, 2, items), Has.Count.EqualTo(1));
            Assert.That(_service.GetItemsForPosition(0, 0, items), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void GetItemsForPosition_PlainEnumerable_FiltersByTypeAndPosition()
    {
        var hit = MakeItem(1, 1);
        var wall = MakeItem(1, 1, type: FurnitureItemType.Wall);
        var far = MakeItem(3, 3);
        var items = new[] { hit, wall, far }.Where(_ => true);

        var result = _service.GetItemsForPosition(1, 1, items);

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0], Is.SameAs(hit));
        });
    }

    [TestCase(-1, 0)]
    [TestCase(0, -1)]
    [TestCase(99, 0)]
    [TestCase(0, 99)]
    public void CanPlaceAt_PointOutsideMap_ReturnsFalseRatherThanThrowing(int x, int y)
    {
        var map = new RoomTileMap("00", []);

        Assert.That(_service.CanPlaceAt([new Point(x, y)], map), Is.False);
    }

    [TestCase(-1, 0)]
    [TestCase(0, -1)]
    [TestCase(99, 0)]
    [TestCase(0, 99)]
    public void CanPlaceAtWithItems_PointOutsideMap_ReturnsFalseRatherThanThrowing(int x, int y)
    {
        var map = new RoomTileMap("00", []);
        var items = new List<PlayerFurnitureItemPlacementDataDto>();

        Assert.That(_service.CanPlaceAt([new Point(x, y)], map, items), Is.False);
    }

    [Test]
    public void CanPlaceAt_UserOnPoint_ReturnsFalse()
    {
        var map = new RoomTileMap("00", []);
        map.AddUnitToMap(new Point(1, 0), Mock.Of<IRoomUnitData>());

        Assert.That(_service.CanPlaceAt([new Point(1, 0)], map), Is.False);
    }

    [Test]
    public void CanPlaceAt_UserOnPointButUsersIgnored_ReturnsTrue()
    {
        var map = new RoomTileMap("00", []);
        map.AddUnitToMap(new Point(1, 0), Mock.Of<IRoomUnitData>());

        Assert.That(_service.CanPlaceAt([new Point(1, 0)], map, checkForUsers: false), Is.True);
    }

    [Test]
    public void CanPlaceAtWithItems_NonStackableTopItem_ReturnsFalse()
    {
        var items = new List<PlayerFurnitureItemPlacementDataDto> { MakeItem(0, 0) };
        var map = new RoomTileMap("0", items);

        Assert.That(_service.CanPlaceAt([new Point(0, 0)], map, items), Is.False);
    }

    [Test]
    public void CanPlaceAtWithItems_StackableTopItem_ReturnsTrue()
    {
        var items = new List<PlayerFurnitureItemPlacementDataDto> { MakeItem(0, 0, canStack: true) };
        var map = new RoomTileMap("0", items);

        Assert.That(_service.CanPlaceAt([new Point(0, 0)], map, items), Is.True);
    }

    [Test]
    public void CanPlaceAtWithItems_UserOnPoint_ReturnsFalse()
    {
        var items = new List<PlayerFurnitureItemPlacementDataDto>();
        var map = new RoomTileMap("0", items);
        map.AddUnitToMap(new Point(0, 0), Mock.Of<IRoomUnitData>());

        Assert.That(_service.CanPlaceAt([new Point(0, 0)], map, items), Is.False);
    }

    [Test]
    public void CanPlaceAtWithItems_OpenTile_ReturnsTrue()
    {
        var items = new List<PlayerFurnitureItemPlacementDataDto>();
        var map = new RoomTileMap("0", items);

        Assert.That(_service.CanPlaceAt([new Point(0, 0)], map, items), Is.True);
    }

    [TestCase(HDirection.North, 5, 4)]
    [TestCase(HDirection.NorthEast, 6, 4)]
    [TestCase(HDirection.East, 6, 5)]
    [TestCase(HDirection.SouthEast, 6, 6)]
    [TestCase(HDirection.South, 5, 6)]
    [TestCase(HDirection.SouthWest, 4, 6)]
    [TestCase(HDirection.West, 4, 5)]
    [TestCase(HDirection.NorthWest, 4, 4)]
    public void GetPointInFront_AllDirections_ReturnsAdjacentPoint(HDirection direction, int expectedX, int expectedY)
    {
        Assert.That(_service.GetPointInFront(5, 5, direction), Is.EqualTo(new Point(expectedX, expectedY)));
    }

    [Test]
    public void GetPointInFront_DiagonalWithOffset_MovesFurther()
    {
        Assert.That(_service.GetPointInFront(5, 5, HDirection.SouthWest, 1), Is.EqualTo(new Point(3, 7)));
    }

    [Test]
    public void GetItemPlacementHeight_NoPoints_ReturnsZero()
    {
        var map = new RoomTileMap("0", []);

        Assert.That(_service.GetItemPlacementHeight(map, [], []), Is.EqualTo(0));
    }
}
