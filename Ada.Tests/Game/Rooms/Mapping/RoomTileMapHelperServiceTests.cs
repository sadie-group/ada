using System.Drawing;
using Ada.API.Collections;
using Ada.API.DTOs.Furniture;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Enums.Game.Rooms.Mapping;
using Ada.Core.Enums.Game.Rooms.Users;
using Ada.Core.Enums.Miscellaneous;
using Ada.Game.Rooms.Mapping;
using Moq;

namespace Ada.Tests.Game.Rooms.Mapping;

[TestFixture]
public class RoomTileMapHelperServiceTests
{
    private IRoomTileMapHelperService _tileMapHelperService;

    [SetUp]
    public void SetUp()
    {
        _tileMapHelperService = new RoomTileMapHelperService();
    }

    [TestCase(HDirection.North, HDirection.South)]
    [TestCase(HDirection.NorthEast, HDirection.SouthWest)]
    [TestCase(HDirection.East, HDirection.West)]
    [TestCase(HDirection.SouthEast, HDirection.NorthWest)]
    [TestCase(HDirection.South, HDirection.North)]
    [TestCase(HDirection.SouthWest, HDirection.NorthEast)]
    [TestCase(HDirection.West, HDirection.East)]
    [TestCase(HDirection.NorthWest, HDirection.SouthEast)]
    public void GetOppositeDirection_ReturnsCorrect(HDirection d, HDirection t)
    {
        var eastResult = _tileMapHelperService.GetOppositeDirection(d);
        Assert.That(eastResult, Is.EqualTo(t));
    }

    [Test]
    public void GetPointsForPlacement_SingleSquare_ReturnsCorrect()
    {
        var points = _tileMapHelperService.GetPointsForPlacement(10, 10, 1, 1, (int) HDirection.North);

        Assert.That(points, Has.Count.EqualTo(1));
        Assert.That(points.First(), Is.EqualTo(new Point(10, 10)));
    }

    [Test]
    public void GetPointsForPlacement_WideItem_ReturnsCorrect()
    {
        var points = _tileMapHelperService.GetPointsForPlacement(10, 10, 1, 5, (int) HDirection.North);

        Assert.Multiple(() =>
        {
            Assert.That(points, Has.Count.EqualTo(5));
            Assert.That(points[0], Is.EqualTo(new Point(10, 10)));
            Assert.That(points[1], Is.EqualTo(new Point(10, 11)));
            Assert.That(points[2], Is.EqualTo(new Point(10, 12)));
            Assert.That(points[3], Is.EqualTo(new Point(10, 13)));
            Assert.That(points[4], Is.EqualTo(new Point(10, 14)));
        });
    }

    [Test]
    public void GetPointsForPlacement_LongItem_ReturnsCorrect()
    {
        var points = _tileMapHelperService.GetPointsForPlacement(10, 10, 3, 1, (int) HDirection.North);

        Assert.That(points, Has.Count.EqualTo(3));

        Assert.Multiple(() =>
        {
            Assert.That(points[0], Is.EqualTo(new Point(10, 10)));
            Assert.That(points[1], Is.EqualTo(new Point(11, 10)));
            Assert.That(points[2], Is.EqualTo(new Point(12, 10)));
        });
    }

    [Test]
    public void GetStateNumberForTile_Open_ReturnsOpen()
    {
        var tileState = _tileMapHelperService.GetTileState(10, 10, new List<PlayerFurnitureItemPlacementDataDto>());
        Assert.That(tileState, Is.EqualTo(RoomTileState.Open));
    }

    [Test]
    public void GetStateNumberForTile_Sit_ReturnsSit()
    {
        var tileState = _tileMapHelperService.GetTileState(10, 10, [
            new PlayerFurnitureItemPlacementDataDto
            {
                PlayerFurnitureItem = new PlayerFurnitureItemDto
                {
                    FurnitureItemId = 0,
                    FurnitureItem = new FurnitureItemDto
                    {
                        CanSit = true,
                        InteractionType = "",
                        Name = "",
                        AssetName = ""
                    },
                    LimitedData = "",
                    MetaData = "",
                },
                PositionX = 10,
                PositionY = 10
            }
        ]);

        Assert.That(tileState, Is.EqualTo(RoomTileState.Sit));
    }

    [Test]
    public void GetStateNumberForTile_Lay_ReturnsLay()
    {
        var tileState = _tileMapHelperService.GetTileState(10, 10, [
            new PlayerFurnitureItemPlacementDataDto
            {
                PlayerFurnitureItem = new PlayerFurnitureItemDto
                {
                    FurnitureItemId = 0,
                    FurnitureItem = new FurnitureItemDto
                    {
                        CanLay = true,
                        InteractionType = "",
                        Name = "",
                        AssetName = ""
                    },
                    LimitedData = "",
                    MetaData = ""
                },
                PositionX = 10,
                PositionY = 10
            }
        ]);

        Assert.That(tileState, Is.EqualTo(RoomTileState.Lay));
    }

    [Test]
    public void GetStateNumberForTile_GateInteractionType_ReturnsOpen()
    {
        var tileState = _tileMapHelperService.GetTileState(10, 10, [
            new PlayerFurnitureItemPlacementDataDto
            {
                PlayerFurnitureItem = new PlayerFurnitureItemDto
                {
                    FurnitureItemId = 0,
                    FurnitureItem = new FurnitureItemDto
                    {
                        CanWalk = false,
                        InteractionType = "gate",
                        Name = "",
                        AssetName = ""
                    },
                    LimitedData = "",
                    MetaData = "1",
                },
                PositionX = 10,
                PositionY = 10
            }
        ]);

        Assert.That(tileState, Is.EqualTo(RoomTileState.Open));
    }

    [Test]
    public void GetItemsForPosition_SingleItem_ReturnsCorrect()
    {
        var someItems = new List<PlayerFurnitureItemPlacementDataDto>
        {
            new() { PositionX = 10, PositionY = 14, PlayerFurnitureItem = new PlayerFurnitureItemDto
                {
                    FurnitureItemId = 0,
                    FurnitureItem = new FurnitureItemDto
                    {
                        Name = "",
                        InteractionType = "default",
                        AssetName = ""
                    },
                    LimitedData = "",
                    MetaData = ""
                }
            }
        };

        Assert.That(_tileMapHelperService.GetItemsForPosition(10, 14, someItems), Has.Count.EqualTo(1));
    }

    [Test]
    public void GetItemsForPosition_NotFound_ReturnsCorrect()
    {
        var someItems = new List<PlayerFurnitureItemPlacementDataDto>
        {
            new() { PositionX = 10, PositionY = 14, PlayerFurnitureItem = new PlayerFurnitureItemDto
                {
                    FurnitureItem = new FurnitureItemDto
                    {
                        Type = FurnitureItemType.Floor,
                        InteractionType = "default",
                        Name = "",
                        AssetName = ""
                    },
                    LimitedData = "",
                    MetaData = "",
                    FurnitureItemId = 0
                }
            }
        };

        Assert.That(_tileMapHelperService.GetItemsForPosition(14, 10, someItems), Is.Empty);
    }

    [Test]
    public void GetItemsForPosition_OverlappingItem_ReturnsAllItems()
    {
        var someItems = new List<PlayerFurnitureItemPlacementDataDto>
        {
            MockFurnitureItem(10, 14),
            MockLongFurnitureItem(10, 13),
            MockFurnitureItem(10, 14),
            MockFurnitureItem(10, 14)
        };

        Assert.That(_tileMapHelperService.GetItemsForPosition(10, 14, someItems), Has.Count.EqualTo(4));
    }

    [Test]
    public void GetWorldArrayFromTileMap_ChairNotGoal_ReturnsBlocked()
    {
        var map = new RoomTileMap("0", [
            MockFurnitureItem(0, 0, 0, 0, "", FurnitureItemType.Floor, false, false, true)
        ]);

        var goalPoint = new Point(4, 4);
        var worldArray = _tileMapHelperService.GetWorldArrayFromTileMap(map, goalPoint, []);

        Assert.That(worldArray[0, 0], Is.EqualTo(0));
    }

    [Test]
    public void GetWorldArrayFromTileMap_ChairGoal_ReturnsOpen()
    {
        var map = new RoomTileMap("0", [
            MockFurnitureItem(0, 0, 0, 0, "", FurnitureItemType.Floor, false, false, true)
        ]);

        var goalPoint = new Point(0, 0);
        var worldArray = _tileMapHelperService.GetWorldArrayFromTileMap(map, goalPoint, []);

        Assert.That(worldArray[0, 0], Is.EqualTo(2));
    }

    [Test]
    public void GetWorldArrayFromTileMap_BedNotGoal_ReturnsBlocked()
    {
        var map = new RoomTileMap("0", [
            MockFurnitureItem(0, 0, 0, 0, "", FurnitureItemType.Floor, false, true)
        ]);

        var goalPoint = new Point(4, 4);
        var worldArray = _tileMapHelperService.GetWorldArrayFromTileMap(map, goalPoint, []);

        Assert.That(worldArray[0, 0], Is.EqualTo(0));
    }

    [Test]
    public void GetWorldArrayFromTileMap_BedGoal_ReturnsOpen()
    {
        var map = new RoomTileMap("0", [
            MockFurnitureItem(0, 0, 0, 0, "", FurnitureItemType.Floor, false, true)
        ]);

        var goalPoint = new Point(0, 0);
        var worldArray = _tileMapHelperService.GetWorldArrayFromTileMap(map, goalPoint, []);

        Assert.That(worldArray[0, 0], Is.EqualTo(3));
    }

    [Test]
    public void GetWorldArrayFromTileMap_TilesWithBlockingFurniture_ReturnsUnwalkable()
    {
        var map = new RoomTileMap("0", [
            MockFurnitureItem()
        ]);

        var worldArray = _tileMapHelperService.GetWorldArrayFromTileMap(map, new Point(), []);

        Assert.That(worldArray[0, 0], Is.EqualTo(0));
    }

    [Test]
    public void GetWorldArrayFromTileMap_TilesWithNonBlockingFurniture_ReturnsWalkable()
    {
        var map = new RoomTileMap("0", [
            MockFurnitureItem(0, 0, 0, 0, "", FurnitureItemType.Floor, true)
        ]);

        var worldArray = _tileMapHelperService.GetWorldArrayFromTileMap(map, new Point(), []);

        Assert.That(worldArray[0, 0], Is.EqualTo(1));
    }

    [Test]
    public void GetWorldArrayFromTileMap_TilesWithUsers_ReturnsUnwalkable()
    {
        var map = new RoomTileMap("0", []);
        var user = new Mock<IRoomUser>();

        map.AddUnitToMap(new Point(), user.Object);

        var worldArray = _tileMapHelperService.GetWorldArrayFromTileMap(map, new Point(), []);

        Assert.That(worldArray[0, 0], Is.EqualTo(0));
    }

    [Test]
    public void GetWorldArrayFromTileMap_EmptyTiles_ReturnsWalkable()
    {
        var map = new RoomTileMap("0", []);
        var worldArray = _tileMapHelperService.GetWorldArrayFromTileMap(map, new Point(), []);

        Assert.That(worldArray[0, 0], Is.EqualTo(1));
    }

    [Test]
    public void GetWorldArrayFromTileMap_BlockingFurnitureOnTileButCanOverride_ReturnsOpen()
    {
        var map = new RoomTileMap("0", [
            MockFurnitureItem()
        ]);

        var worldArray = _tileMapHelperService.GetWorldArrayFromTileMap(map,
            new Point(),
            [new Point()]);

        Assert.That(worldArray[0, 0], Is.EqualTo(1));
    }

    [Test]
    public void CanPlaceAt_OpenPoint_ReturnsTrue()
    {
        var map = new RoomTileMap("0", []);
        var points = new Point[] { new () };

        Assert.That(_tileMapHelperService.CanPlaceAt(points, map), Is.EqualTo(true));
    }

    [Test]
    public void CanPlaceAt_OpenPoints_ReturnsTrue()
    {
        var map = new RoomTileMap("00", []);
        var points = new Point[] { new (), new(1, 0) };

        Assert.That(_tileMapHelperService.CanPlaceAt(points, map), Is.EqualTo(true));
    }

    [Test]
    public void CanPlaceAt_HasNonStackableFurniture_ReturnsFalse()
    {
        var map = new RoomTileMap("0", [
            MockFurnitureItem(0, 0, 10, 4)
        ]);

        var points = new Point[] { new () };

        Assert.That(_tileMapHelperService.CanPlaceAt(points, map), Is.EqualTo(false));
    }

    [Test]
    public void GetUsersAtPoints_PointWithUser_Exists()
    {
        var points = new Point[] { new () };

        var userMock = new Mock<IRoomUser>();
        var usersInRoom = new List<IRoomUser> { userMock.Object };
        var usersAtPoints = _tileMapHelperService.GetUsersAtPoints(points, usersInRoom);

        Assert.That(usersAtPoints, Has.Count.EqualTo(1));
    }

    [Test]
    public void GetUsersAtPoints_PointWithoutUser_ReturnsZero()
    {
        var points = new Point[] { new () };

        var userMock = new Mock<IRoomUser>();

        userMock
            .SetupGet(u => u.Point)
            .Returns(new Point(4,4));

        var usersInRoom = new List<IRoomUser> { userMock.Object };
        var usersAtPoints = _tileMapHelperService.GetUsersAtPoints(points, usersInRoom);

        Assert.That(usersAtPoints, Has.Count.EqualTo(0));
    }

    [Test]
    public void GetUsersAtPoints_PointsWithUser_ReturnsCorrect()
    {
        var points = new Point[]
        {
            new (6, 10),
            new (8, 9),
            new (5, 0),
            new (4, 1)
        };

        var userMock1 = new Mock<IRoomUser>();
        var userMock2 = new Mock<IRoomUser>();
        var userMock3 = new Mock<IRoomUser>();
        var userMock4 = new Mock<IRoomUser>();

        userMock1
            .SetupGet(u => u.Point)
            .Returns(new Point(6,10));

        userMock2
            .SetupGet(u => u.Point)
            .Returns(new Point(8,9));

        userMock3
            .SetupGet(u => u.Point)
            .Returns(new Point(5,0));

        userMock4
            .SetupGet(u => u.Point)
            .Returns(new Point(4,1));

        var usersInRoom = new List<IRoomUser>
        {
            userMock1.Object,
            userMock2.Object,
            userMock3.Object,
            userMock4.Object
        };

        var usersAtPoints = _tileMapHelperService.GetUsersAtPoints(points, usersInRoom);

        Assert.That(usersAtPoints, Has.Count.EqualTo(4));
    }

    [Test]
    public void GetPointInFront_North_ReturnsCorrect()
    {
        Assert.That(_tileMapHelperService.GetPointInFront(1, 2, HDirection.North), Is.EqualTo(new Point(1, 1)));
    }

    [Test]
    public void GetPointInFront_East_ReturnsCorrect()
    {
        Assert.That(_tileMapHelperService.GetPointInFront(1, 2, HDirection.East), Is.EqualTo(new Point(2, 2)));
    }

    [Test]
    public void GetPointInFront_South_ReturnsCorrect()
    {
        Assert.That(_tileMapHelperService.GetPointInFront(1, 2, HDirection.South), Is.EqualTo(new Point(1, 3)));
    }

    [Test]
    public void GetPointInFront_West_ReturnsCorrect()
    {
        Assert.That(_tileMapHelperService.GetPointInFront(1, 2, HDirection.West), Is.EqualTo(new Point(0, 2)));
    }

    [Test]
    public void GetPointInFront_NorthWithOffset_ReturnsCorrect()
    {
        Assert.That(_tileMapHelperService.GetPointInFront(1, 2, HDirection.North, 5), Is.EqualTo(new Point(1, -4)));
    }

    [Test]
    public void GetItemPlacementHeight_SingleTileItem_ReturnsCorrect()
    {
        var furnitureItems = new List<PlayerFurnitureItemPlacementDataDto>();
        var map = new RoomTileMap("0", furnitureItems);
        var pointsForPlacement = new Point[] { new () };

        Assert.That(_tileMapHelperService.GetItemPlacementHeight(map, pointsForPlacement, furnitureItems), Is.EqualTo(map.ZMap[0, 0]));
    }

    [Test]
    public void GetItemPlacementHeight_ExcludedItem_IsNotStackedOn()
    {
        var moving = MockFurnitureItem(0, 0, 20, 4);

        var furnitureItems = new List<PlayerFurnitureItemPlacementDataDto>
        {
            MockFurnitureItem(0, 0, 10, 4),
            moving
        };

        var map = new RoomTileMap("0", furnitureItems);

        var height = _tileMapHelperService.GetItemPlacementHeight(map, [new Point(0, 0)], furnitureItems, moving);

        Assert.That(height, Is.EqualTo(10 + 4));
    }

    [Test]
    public void GetItemPlacementHeight_ExcludedItemIsTheOnlyOne_FallsBackToTheHeightmap()
    {
        var moving = MockFurnitureItem(0, 0, 20, 4);
        var furnitureItems = new List<PlayerFurnitureItemPlacementDataDto> { moving };
        var map = new RoomTileMap("0", furnitureItems);

        var height = _tileMapHelperService.GetItemPlacementHeight(map, [new Point(0, 0)], furnitureItems, moving);

        Assert.That(height, Is.EqualTo(map.ZMap[0, 0]));
    }

    [Test]
    public void GetItemPlacementHeight_StackedSingleTileItems_ReturnsCorrect()
    {
        var furnitureItems = new List<PlayerFurnitureItemPlacementDataDto>
        {
            MockFurnitureItem(0, 0, 10, 4),
            MockFurnitureItem(0, 0, 20, 4)
        };

        var map = new RoomTileMap("0", furnitureItems);
        var pointsForPlacement = new Point[] { new () };
        var z = _tileMapHelperService.GetItemPlacementHeight(map, pointsForPlacement, furnitureItems);

        Assert.That(z, Is.EqualTo(24));
    }

    [Test]
    public void GetSquaresBetweenPoints_NextTile_ReturnsCorrect()
    {
        Assert.That(_tileMapHelperService.GetSquaresBetweenPoints(new Point(), new Point(0, 1)), Is.EqualTo(1));
    }

    [Test]
    public void GetSquaresBetweenPoints_FarAwayTile_ReturnsCorrect()
    {
        Assert.That(_tileMapHelperService.GetSquaresBetweenPoints(new Point(), new Point(8, 2)), Is.EqualTo(10));
    }

    [Test]
    public void GetEffectFromInteractionType_Water_ReturnsSwimming()
    {
        var effect =
            _tileMapHelperService.GetEffectFromInteractionType(FurnitureItemInteractionType.Water);
        Assert.That(effect, Is.EqualTo(RoomUserEffect.Swimming));
    }

    [Test]
    public void UpdateTileMapsForPoints_BlockingItemPlaced_MarkedAsBlocked()
    {
        var furnitureItems = new List<PlayerFurnitureItemPlacementDataDto>
        {
            MockFurnitureItem(x: 1, y: 0, canWalk: false)
        };

        var map = new RoomTileMap("00", furnitureItems);

        Assert.That(map.Map[0, 0], Is.EqualTo((int) RoomTileState.Open));

        furnitureItems[0].PositionX = 0;

        _tileMapHelperService.UpdateTileMapsForPoints([new Point(0, 0)], map, furnitureItems);

        Assert.That(map.Map[0, 0], Is.EqualTo((int) RoomTileState.Blocked));
    }

    [Test]
    public void GetItemsForPosition_ItemSwappedWithoutCountChange_SeesTheNewItem()
    {
        var items = new RevisionedCollection<PlayerFurnitureItemPlacementDataDto>
        {
            MockFurnitureItem(3, 3)
        };

        Assert.That(_tileMapHelperService.GetItemsForPosition(3, 3, items), Has.Count.EqualTo(1));

        items.Remove(items.First());
        items.Add(MockFurnitureItem(7, 7));

        Assert.Multiple(() =>
        {
            Assert.That(_tileMapHelperService.GetItemsForPosition(3, 3, items), Is.Empty);
            Assert.That(_tileMapHelperService.GetItemsForPosition(7, 7, items), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void TryMoveItemInIndex_NoIndexBuiltYet_ReportsIndexNotCurrent()
    {
        var item = MockFurnitureItem(1, 1);
        var items = new RevisionedCollection<PlayerFurnitureItemPlacementDataDto> { item };

        var moved = _tileMapHelperService.TryMoveItemInIndex(
            items, item, [new Point(1, 1)], [new Point(2, 1)]);

        Assert.That(moved, Is.False, "there is no index to absorb the move, so the caller must invalidate");
    }

    [Test]
    public void TryMoveItemInIndex_RelocatesItemWithoutRebuilding()
    {
        var item = MockFurnitureItem(1, 1);
        var items = new RevisionedCollection<PlayerFurnitureItemPlacementDataDto> { item };

        _tileMapHelperService.GetItemsForPosition(1, 1, items);

        item.PositionX = 2;

        var moved = _tileMapHelperService.TryMoveItemInIndex(
            items, item, [new Point(1, 1)], [new Point(2, 1)]);

        Assert.Multiple(() =>
        {
            Assert.That(moved, Is.True);
            Assert.That(_tileMapHelperService.GetItemsForPosition(1, 1, items), Is.Empty);
            Assert.That(_tileMapHelperService.GetItemsForPosition(2, 1, items), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void TryMoveItemInIndex_CollectionChangedSinceIndexBuilt_ReportsIndexNotCurrent()
    {
        var item = MockFurnitureItem(1, 1);
        var items = new RevisionedCollection<PlayerFurnitureItemPlacementDataDto> { item };

        _tileMapHelperService.GetItemsForPosition(1, 1, items);

        items.Add(MockFurnitureItem(5, 5));
        items.Remove(items.First(x => x.PositionX == 5));

        var moved = _tileMapHelperService.TryMoveItemInIndex(
            items, item, [new Point(1, 1)], [new Point(2, 1)]);

        Assert.That(moved, Is.False);
    }

    [Test]
    public void TryMoveItemInIndex_WallItem_ReportsIndexCurrent()
    {
        var item = MockFurnitureItem(1, 1, type: FurnitureItemType.Wall);
        var items = new RevisionedCollection<PlayerFurnitureItemPlacementDataDto> { item };

        var moved = _tileMapHelperService.TryMoveItemInIndex(
            items, item, [new Point(1, 1)], [new Point(2, 1)]);

        Assert.That(moved, Is.True, "wall items are not indexed by tile, so the index is already correct");
    }

    [Test]
    public void TryMoveItemInIndex_DoesNotMutateAListAlreadyHandedToAReader()
    {
        var item = MockFurnitureItem(1, 1);
        var items = new RevisionedCollection<PlayerFurnitureItemPlacementDataDto> { item };

        var readBeforeMove = _tileMapHelperService.GetItemsForPosition(1, 1, items);

        item.PositionX = 2;
        _tileMapHelperService.TryMoveItemInIndex(items, item, [new Point(1, 1)], [new Point(2, 1)]);

        Assert.That(readBeforeMove, Has.Count.EqualTo(1),
            "a reader's result must not change under it when the index absorbs a move");
    }

    [Test]
    public void TryMoveItemInIndex_LeavesUntouchedTilesAlone()
    {
        var moving = MockFurnitureItem(1, 1);
        var bystander = MockFurnitureItem(8, 8);

        var items = new RevisionedCollection<PlayerFurnitureItemPlacementDataDto> { moving, bystander };

        var bystanderBefore = _tileMapHelperService.GetItemsOnTilePosition(8, 8, items);

        moving.PositionX = 2;

        var moved = _tileMapHelperService.TryMoveItemInIndex(
            items, moving, [new Point(1, 1)], [new Point(2, 1)]);

        Assert.Multiple(() =>
        {
            Assert.That(moved, Is.True);
            Assert.That(_tileMapHelperService.GetItemsOnTilePosition(8, 8, items), Is.SameAs(bystanderBefore),
                "an unrelated tile must not be reallocated to absorb a move elsewhere");
            Assert.That(_tileMapHelperService.GetItemsOnTilePosition(1, 1, items), Is.Empty);
            Assert.That(_tileMapHelperService.GetItemsOnTilePosition(2, 1, items), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void TryMoveItemInIndex_MultiTileItem_UpdatesEveryTileOfTheFootprint()
    {
        var item = MockLongFurnitureItem(1, 1);
        var items = new RevisionedCollection<PlayerFurnitureItemPlacementDataDto> { item };

        var oldPoints = _tileMapHelperService.GetPointsForPlacement(
            1, 1,
            item.PlayerFurnitureItem.FurnitureItem.TileSpanX,
            item.PlayerFurnitureItem.FurnitureItem.TileSpanY,
            item.Direction);

        _tileMapHelperService.GetItemsOnTilePosition(oldPoints[0].X, oldPoints[0].Y, items);

        item.PositionX = 5;
        item.PositionY = 5;

        var newPoints = _tileMapHelperService.GetPointsForPlacement(
            5, 5,
            item.PlayerFurnitureItem.FurnitureItem.TileSpanX,
            item.PlayerFurnitureItem.FurnitureItem.TileSpanY,
            item.Direction);

        Assert.That(_tileMapHelperService.TryMoveItemInIndex(items, item, oldPoints, newPoints), Is.True);

        Assert.Multiple(() =>
        {
            foreach (var point in oldPoints)
            {
                Assert.That(_tileMapHelperService.GetItemsOnTilePosition(point.X, point.Y, items), Is.Empty,
                    $"tile {point} should no longer carry the item");
            }

            foreach (var point in newPoints)
            {
                Assert.That(_tileMapHelperService.GetItemsOnTilePosition(point.X, point.Y, items),
                    Has.Count.EqualTo(1), $"tile {point} should now carry the item");
            }
        });
    }

    private static PlayerFurnitureItemPlacementDataDto MockLongFurnitureItem(int x = 0,
        int y = 0)
    {
        var item = MockFurnitureItem(x, y);
        item.PlayerFurnitureItem.FurnitureItem.TileSpanY = 2;

        return item;
    }

    private static PlayerFurnitureItemPlacementDataDto MockFurnitureItem(int x = 0,
        int y = 0,
        int z = 0,
        int stackHeight = 0,
        string interactionType = "",
        FurnitureItemType type = FurnitureItemType.Floor,
        bool canWalk = false,
        bool canLay = false,
        bool canSit = false)
    {
        return new PlayerFurnitureItemPlacementDataDto
        {
            PlayerFurnitureItem = new PlayerFurnitureItemDto
            {
                LimitedData = "",
                MetaData = "",
                FurnitureItem = new FurnitureItemDto
                {
                    StackHeight = stackHeight,
                    InteractionType = interactionType,
                    Type = type,
                    TileSpanX = 1,
                    TileSpanY = 1,
                    CanWalk = canWalk,
                    CanLay = canLay,
                    CanSit = canSit,
                    Name = "",
                    AssetName = ""
                },
                FurnitureItemId = 0
            },
            PositionX = x,
            PositionY = y,
            PositionZ = z
        };
    }
}
