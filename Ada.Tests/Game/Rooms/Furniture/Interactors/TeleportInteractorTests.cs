using System.Collections.Concurrent;
using System.Drawing;
using Ada.API;
using Ada.API.DTOs.Furniture;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Unit;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Networking;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Enums.Miscellaneous;
using Ada.Db.Models.Furniture;
using Ada.Db.Models.Players;
using Ada.Db.Models.Players.Furniture;
using Ada.Db.Models.Rooms;
using Ada.Game.Rooms.Furniture.Interactors;
using Ada.Networking.Writers.Rooms.Users;
using Ada.Tests.Common;
using AutoMapper;
using Moq;

namespace Ada.Tests.Game.Rooms.Furniture.Interactors;

[TestFixture]
public class TeleportInteractorTests
{
    private sealed class TestUser
    {
        public required Mock<IRoomUser> User { get; init; }
        public required Mock<IPlayerState> State { get; init; }
        public required Mock<INetworkObject> NetworkObject { get; init; }
        public Point Point { get; set; }
    }

    private sealed class Harness
    {
        public required TeleportInteractor Interactor { get; init; }
        public required Mock<IRoomRepository> RoomRepository { get; init; }
        public required Mock<IRoomTileMapHelperService> TileHelper { get; init; }
        public required Mock<IRoomFurnitureItemHelperService> FurniHelper { get; init; }
    }

    private static Harness MakeHarness(SqliteTestDbFactory factory)
    {
        var roomRepository = new Mock<IRoomRepository>();
        var tileHelper = new Mock<IRoomTileMapHelperService>();
        var furniHelper = new Mock<IRoomFurnitureItemHelperService>();

        return new Harness
        {
            Interactor = new TeleportInteractor(
                roomRepository.Object,
                factory,
                Mock.Of<IMapper>(),
                tileHelper.Object,
                furniHelper.Object,
                new InlineRoomDeferralScheduler(),
                NullLogger<TeleportInteractor>.Instance),
            RoomRepository = roomRepository,
            TileHelper = tileHelper,
            FurniHelper = furniHelper
        };
    }

    private static TestUser MakeUser(Point start)
    {
        var state = new Mock<IPlayerState>();
        state.SetupAllProperties();

        var player = new Mock<IPlayerLogic>();
        player.SetupGet(x => x.State).Returns(state.Object);

        var networkObject = new Mock<INetworkObject>();

        var user = new Mock<IRoomUser>();
        user.SetupAllProperties();
        user.SetupGet(x => x.Player).Returns(player.Object);
        user.SetupGet(x => x.NetworkObject).Returns(networkObject.Object);

        var testUser = new TestUser { User = user, State = state, NetworkObject = networkObject, Point = start };

        user.SetupGet(x => x.Point).Returns(() => testUser.Point);
        user.Setup(x => x.WalkToPoint(It.IsAny<Point>(), It.IsAny<Action?>()))
            .Callback<Point, Action?>((point, onReachedGoal) =>
            {
                testUser.Point = point;
                onReachedGoal?.Invoke();
            });
        user.Object.OverridePoints = [];

        return testUser;
    }

    private static (Mock<IRoomLogic> Room, Mock<IRoomTileMap> TileMap, ConcurrentDictionary<Point, List<IRoomUnitData>> UnitMap) MakeRoom(RoomDto dto)
    {
        var unitMap = new ConcurrentDictionary<Point, List<IRoomUnitData>>();
        var tileMap = new Mock<IRoomTileMap>();
        tileMap.SetupGet(x => x.UnitMap).Returns(unitMap);

        var room = new Mock<IRoomLogic>();
        room.SetupGet(x => x.Room).Returns(dto);
        room.SetupGet(x => x.TileMap).Returns(tileMap.Object);
        room.Setup(x => x.RunLockedAsync(It.IsAny<Func<Task>>())).Returns<Func<Task>>(action => action());

        return (room, tileMap, unitMap);
    }

    private static PlayerFurnitureItemPlacementDataDto MakeItem(int itemId, int x, int y, HDirection direction, int interactionModes)
    {
        return new PlayerFurnitureItemPlacementDataDto
        {
            Id = itemId,
            PlayerFurnitureItemId = itemId,
            PlayerFurnitureItem = new PlayerFurnitureItemDto
            {
                FurnitureItem = new FurnitureItemDto
                {
                    Name = "teleport",
                    AssetName = "teleport",
                    InteractionType = FurnitureItemInteractionType.Teleport,
                    InteractionModes = interactionModes
                },
                FurnitureItemId = 10,
                LimitedData = "",
                MetaData = "0"
            },
            PositionX = x,
            PositionY = y,
            Direction = direction
        };
    }

    private static async Task SeedLinkAsync(SqliteTestDbFactory factory, int parentId, int childId, int? targetRoomId = null)
    {
        await using var db = factory.CreateDbContext();

        var player = new Player { Id = 1, Username = "owner", Email = "e", Password = "p" };
        var furni = new FurnitureItem { Id = 10, Name = "teleport", AssetName = "teleport", InteractionType = "teleport", InteractionModes = 1 };
        var parent = new PlayerFurnitureItem { Id = parentId, Player = player, FurnitureItemId = 10, FurnitureItem = furni, LimitedData = "", MetaData = "0" };
        var child = new PlayerFurnitureItem { Id = childId, Player = player, FurnitureItemId = 10, FurnitureItem = furni, LimitedData = "", MetaData = "0" };

        db.AddRange(player, furni, parent, child);
        db.PlayerFurnitureItemLinks.Add(new PlayerFurnitureItemLink { ParentId = parentId, ChildId = childId });

        if (targetRoomId != null)
        {
            db.RoomLayouts.Add(new RoomLayout { Id = 1 });
            db.Rooms.Add(new Room { Id = targetRoomId.Value, Name = "target", Description = "", OwnerId = 1, LayoutId = 1 });
            db.RoomFurnitureItems.Add(new PlayerFurnitureItemPlacementData
            {
                Id = 300,
                PlayerFurnitureItemId = childId,
                PlayerFurnitureItem = child,
                RoomId = targetRoomId.Value
            });
        }

        await db.SaveChangesAsync();
    }

    [Test]
    public void InteractionTypes_ContainsTeleport()
    {
        using var factory = new SqliteTestDbFactory();

        Assert.That(MakeHarness(factory).Interactor.InteractionTypes, Is.EqualTo(new[] { FurnitureItemInteractionType.Teleport }));
    }

    [Test]
    public async Task OnTriggerAsync_UserOnItemWithoutLink_FacesOppositeAndCloses()
    {
        using var factory = new SqliteTestDbFactory();
        var harness = MakeHarness(factory);
        harness.TileHelper.Setup(x => x.GetOppositeDirection(HDirection.North)).Returns(HDirection.South);

        var item = MakeItem(100, 1, 1, HDirection.North, 1);
        var (room, _, _) = MakeRoom(new RoomDto { Id = 1, FurnitureItems = [item] });
        var user = MakeUser(new Point(1, 1));

        await harness.Interactor.OnTriggerAsync(room.Object, item, user.User.Object);
        Assert.Multiple(() =>
        {
            Assert.That(user.User.Object.CanWalk, Is.False);
            Assert.That(user.User.Object.Direction, Is.EqualTo(HDirection.South));
            Assert.That(user.User.Object.DirectionHead, Is.EqualTo(HDirection.South));
            Assert.That(user.User.Object.NeedsUpdate, Is.True);
        });
        harness.FurniHelper.Verify(x => x.UpdateMetaDataForItemAsync(room.Object, item, "1"), Times.Once);
    }

    [Test]
    public async Task OnTriggerAsync_UserFarAway_WalksToFrontThenEntersTeleport()
    {
        using var factory = new SqliteTestDbFactory();
        var harness = MakeHarness(factory);
        harness.TileHelper.Setup(x => x.GetPointInFront(1, 1, HDirection.North, It.IsAny<int>())).Returns(new Point(1, 0));

        var item = MakeItem(100, 1, 1, HDirection.North, 1);
        var (room, _, _) = MakeRoom(new RoomDto { Id = 1, FurnitureItems = [item] });
        var user = MakeUser(new Point(5, 5));

        await harness.Interactor.OnTriggerAsync(room.Object, item, user.User.Object);
        await Task.Delay(1200);
        Assert.Multiple(() =>
        {
            Assert.That(user.Point, Is.EqualTo(new Point(1, 1)));
            Assert.That(user.User.Object.CanWalk, Is.False);
            Assert.That(user.User.Object.OverridePoints, Is.Empty);
        });
        harness.FurniHelper.Verify(x => x.UpdateMetaDataForItemAsync(room.Object, item, "1"), Times.Once);
        harness.FurniHelper.Verify(x => x.UpdateMetaDataForItemAsync(room.Object, item, "2"), Times.Once);
        harness.FurniHelper.Verify(x => x.UpdateMetaDataForItemAsync(room.Object, item, "0"), Times.Once);
    }

    [Test]
    public async Task OnTriggerAsync_LinkedItemInSameRoom_MovesUserToTargetAndWalksOff()
    {
        using var factory = new SqliteTestDbFactory();
        await SeedLinkAsync(factory, 100, 200);

        var harness = MakeHarness(factory);
        harness.TileHelper.Setup(x => x.GetOppositeDirection(HDirection.North)).Returns(HDirection.South);
        harness.TileHelper.Setup(x => x.GetPointInFront(4, 4, HDirection.East, It.IsAny<int>())).Returns(new Point(5, 4));

        var item = MakeItem(100, 1, 1, HDirection.North, 1);
        var target = MakeItem(200, 4, 4, HDirection.East, 1);
        var (room, tileMap, unitMap) = MakeRoom(new RoomDto { Id = 1, FurnitureItems = [item, target] });

        var user = MakeUser(new Point(1, 1));
        unitMap[new Point(1, 1)] = [user.User.Object];

        await harness.Interactor.OnTriggerAsync(room.Object, item, user.User.Object);
        await Task.Delay(1200);

        Assert.That(unitMap[new Point(1, 1)], Is.Empty);
        tileMap.Verify(x => x.AddUnitToMap(new Point(4, 4), user.User.Object), Times.Once);
        user.User.Verify(x => x.SetPositionAsync(new Point(4, 4)), Times.Once);
        Assert.Multiple(() =>
        {
            Assert.That(user.User.Object.Direction, Is.EqualTo(HDirection.East));
            Assert.That(user.User.Object.CanWalk, Is.True);
            Assert.That(user.Point, Is.EqualTo(new Point(5, 4)));
        });
        harness.FurniHelper.Verify(x => x.UpdateMetaDataForItemAsync(room.Object, item, "1"), Times.Once);
        harness.FurniHelper.Verify(x => x.UpdateMetaDataForItemAsync(room.Object, target, "2"), Times.Once);
        harness.FurniHelper.Verify(x => x.UpdateMetaDataForItemAsync(room.Object, target, "1"), Times.Once);
        harness.FurniHelper.Verify(x => x.UpdateMetaDataForItemAsync(room.Object, item, "0"), Times.Once);
        harness.FurniHelper.Verify(x => x.UpdateMetaDataForItemAsync(room.Object, target, "0"), Times.Once);
    }

    [Test]
    public async Task OnTriggerAsync_LinkedItemInOtherLoadedRoom_ForwardsUser()
    {
        using var factory = new SqliteTestDbFactory();
        await SeedLinkAsync(factory, 100, 200, targetRoomId: 2);

        var harness = MakeHarness(factory);
        harness.TileHelper.Setup(x => x.GetOppositeDirection(HDirection.North)).Returns(HDirection.South);

        var item = MakeItem(100, 1, 1, HDirection.North, 1);
        var target = MakeItem(200, 4, 4, HDirection.East, 1);
        var (room, _, _) = MakeRoom(new RoomDto { Id = 1, FurnitureItems = [item] });
        var (targetRoom, _, _) = MakeRoom(new RoomDto { Id = 2, FurnitureItems = [target] });
        harness.RoomRepository.Setup(x => x.TryGetRoomById(2)).Returns(targetRoom.Object);

        var user = MakeUser(new Point(1, 1));

        await harness.Interactor.OnTriggerAsync(room.Object, item, user.User.Object);

        Assert.That(user.State.Object.Teleport, Is.SameAs(target));
        user.NetworkObject.Verify(x => x.WriteToStreamAsync(
            It.Is<AbstractPacketWriter>(w => ((RoomForwardEntryWriter)w).RoomId == 2)), Times.Once);
        harness.FurniHelper.Verify(x => x.UpdateMetaDataForItemAsync(targetRoom.Object, target, "2"), Times.Once);
        harness.FurniHelper.Verify(x => x.UpdateMetaDataForItemAsync(targetRoom.Object, target, "1"), Times.Once);
        harness.FurniHelper.Verify(x => x.UpdateMetaDataForItemAsync(room.Object, item, "0"), Times.Once);
    }

    [Test]
    public async Task OnTriggerAsync_TargetRoomMissingItem_DoesNotForward()
    {
        using var factory = new SqliteTestDbFactory();
        await SeedLinkAsync(factory, 100, 200, targetRoomId: 2);

        var harness = MakeHarness(factory);
        harness.TileHelper.Setup(x => x.GetOppositeDirection(HDirection.North)).Returns(HDirection.South);

        var item = MakeItem(100, 1, 1, HDirection.North, 1);
        var (room, _, _) = MakeRoom(new RoomDto { Id = 1, FurnitureItems = [item] });
        var (targetRoom, _, _) = MakeRoom(new RoomDto { Id = 2 });
        harness.RoomRepository.Setup(x => x.TryGetRoomById(2)).Returns(targetRoom.Object);

        var user = MakeUser(new Point(1, 1));

        await harness.Interactor.OnTriggerAsync(room.Object, item, user.User.Object);

        Assert.That(user.State.Object.Teleport, Is.Null);
        user.NetworkObject.Verify(x => x.WriteToStreamAsync(It.IsAny<AbstractPacketWriter>()), Times.Never);
    }
}
