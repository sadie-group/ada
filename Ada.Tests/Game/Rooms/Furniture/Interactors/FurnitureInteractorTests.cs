using System.Drawing;
using Ada.API.DTOs;
using Ada.API.DTOs.Furniture;
using Ada.API.DTOs.Players;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Networking;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Enums.Miscellaneous;
using Ada.Db.Models.Furniture;
using Ada.Db.Models.Players;
using Ada.Db.Models.Players.Furniture;
using Ada.Game.Rooms.Furniture.Interactors;
using Ada.Networking.Writers.Rooms.Users.HandItems;
using Ada.Tests.Common;
using Moq;

namespace Ada.Tests.Game.Rooms.Furniture.Interactors;

internal static class InteractorTestHelpers
{
    public sealed class TestUser
    {
        public required Mock<IRoomUser> User { get; init; }
        public Point Point { get; set; }
    }

    public sealed class TestRoom
    {
        public required Mock<IRoomLogic> Room { get; init; }
        public required Mock<IRoomTileMap> TileMap { get; init; }
        public required Mock<IRoomUserRepository> UserRepository { get; init; }
        public required List<AbstractPacketWriter> Broadcasts { get; init; }
    }

    public static PlayerDto MakePlayerDto(long id, ICollection<PlayerSubscriptionDto>? subscriptions = null)
    {
        return new PlayerDto(
            id,
            "user" + id,
            "test@example.com",
            DateTimeOffset.UtcNow,
            [],
            new PlayerDataDto(),
            new PlayerAvatarDataDto { FigureCode = "figure", Motto = "motto" },
            [],
            [],
            [],
            [],
            new PlayerNavigatorSettingsDto(),
            new PlayerGameSettingsDto(),
            [],
            [],
            [],
            subscriptions ?? [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            []);
    }

    public static TestUser MakeUser(Point start, PlayerDto playerDto)
    {
        var player = new Mock<IPlayerLogic>();
        player.SetupGet(x => x.Player).Returns(playerDto);

        var user = new Mock<IRoomUser>();
        user.SetupAllProperties();
        user.SetupGet(x => x.Player).Returns(player.Object);

        var testUser = new TestUser { User = user, Point = start };

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

    public static TestRoom MakeRoom(short[,]? map = null)
    {
        var tileMap = new Mock<IRoomTileMap>();
        tileMap.SetupGet(x => x.Map).Returns(map ?? new short[8, 8]);

        var userRepository = new Mock<IRoomUserRepository>();
        userRepository.Setup(x => x.GetAll()).Returns(new List<IRoomUser>());

        var broadcasts = new List<AbstractPacketWriter>();

        var room = new Mock<IRoomLogic>();
        room.SetupGet(x => x.TileMap).Returns(tileMap.Object);
        room.SetupGet(x => x.UserRepository).Returns(userRepository.Object);
        room.Setup(x => x.BroadcastDataAsync(It.IsAny<AbstractPacketWriter>(), It.IsAny<IReadOnlyCollection<long>?>()))
            .Callback<AbstractPacketWriter, IReadOnlyCollection<long>?>((writer, _) =>
            {
                lock (broadcasts)
                {
                    broadcasts.Add(writer);
                }
            })
            .Returns(Task.CompletedTask);
        room.Setup(x => x.RunLockedAsync(It.IsAny<Func<Task>>())).Returns<Func<Task>>(action => action());

        return new TestRoom { Room = room, TileMap = tileMap, UserRepository = userRepository, Broadcasts = broadcasts };
    }

    public static PlayerFurnitureItemPlacementDataDto MakeItem(int itemId, int x, int y, HDirection direction,
        string interactionType, string metaData = "0", ICollection<HandItemDto>? handItems = null)
    {
        return new PlayerFurnitureItemPlacementDataDto
        {
            Id = itemId,
            PlayerFurnitureItemId = itemId,
            PlayerFurnitureItem = new PlayerFurnitureItemDto
            {
                Id = itemId,
                FurnitureItem = new FurnitureItemDto
                {
                    Name = "furni",
                    AssetName = "furni",
                    InteractionType = interactionType,
                    InteractionModes = 1,
                    HandItems = handItems ?? []
                },
                FurnitureItemId = 10,
                LimitedData = "",
                MetaData = metaData
            },
            PositionX = x,
            PositionY = y,
            Direction = direction
        };
    }

    public static async Task SeedItemRowAsync(SqliteTestDbFactory factory, int itemId, string metaData)
    {
        await using var db = factory.CreateDbContext();

        var player = new Player { Id = 1, Username = "owner", Email = "e", Password = "p" };
        var furni = new FurnitureItem
            { Id = 10, Name = "furni", AssetName = "furni", InteractionType = "gate", InteractionModes = 1 };

        db.AddRange(player, furni, new PlayerFurnitureItem
            { Id = itemId, Player = player, FurnitureItemId = 10, FurnitureItem = furni, LimitedData = "", MetaData = metaData });

        await db.SaveChangesAsync();
    }
}

[TestFixture]
public class OneWayGateInteractorTests
{
    private static (OneWayGateInteractor Interactor, Mock<IRoomTileMapHelperService> TileHelper,
        Mock<IRoomFurnitureItemHelperService> FurniHelper) MakeInteractor(SqliteTestDbFactory factory)
    {
        var tileHelper = new Mock<IRoomTileMapHelperService>();
        var furniHelper = new Mock<IRoomFurnitureItemHelperService>();

        return (new OneWayGateInteractor(factory, tileHelper.Object, furniHelper.Object,
            NullLogger<OneWayGateInteractor>.Instance), tileHelper, furniHelper);
    }

    [Test]
    public void InteractionTypes_ContainsOneWayGate()
    {
        using var factory = new SqliteTestDbFactory();

        Assert.That(MakeInteractor(factory).Interactor.InteractionTypes,
            Is.EqualTo(new[] { FurnitureItemInteractionType.OneWayGate }));
    }

    [Test]
    public async Task OnTriggerAsync_UserNotInFront_DoesNothing()
    {
        using var factory = new SqliteTestDbFactory();
        var (interactor, tileHelper, furniHelper) = MakeInteractor(factory);
        tileHelper.Setup(x => x.GetPointInFront(2, 2, HDirection.North, It.IsAny<int>())).Returns(new Point(2, 1));

        var item = InteractorTestHelpers.MakeItem(100, 2, 2, HDirection.North, FurnitureItemInteractionType.OneWayGate);
        var room = InteractorTestHelpers.MakeRoom();
        var user = InteractorTestHelpers.MakeUser(new Point(5, 5), InteractorTestHelpers.MakePlayerDto(1));

        await interactor.OnTriggerAsync(room.Room.Object, item, user.User.Object);

        furniHelper.Verify(x => x.UpdateMetaDataForItemAsync(It.IsAny<IRoomLogic>(),
            It.IsAny<PlayerFurnitureItemPlacementDataDto>(), It.IsAny<string>()), Times.Never);
        user.User.Verify(x => x.WalkToPoint(It.IsAny<Point>(), It.IsAny<Action?>()), Times.Never);
    }

    [Test]
    public async Task OnTriggerAsync_NoTileBehind_DoesNothing()
    {
        using var factory = new SqliteTestDbFactory();
        var (interactor, tileHelper, furniHelper) = MakeInteractor(factory);
        tileHelper.Setup(x => x.GetPointInFront(2, 2, HDirection.North, It.IsAny<int>())).Returns(new Point(2, 1));
        tileHelper.Setup(x => x.GetOppositeDirection(HDirection.North)).Returns(HDirection.South);
        tileHelper.Setup(x => x.GetPointInFront(2, 2, HDirection.South, It.IsAny<int>())).Returns(new Point(2, 3));

        var item = InteractorTestHelpers.MakeItem(100, 2, 2, HDirection.North, FurnitureItemInteractionType.OneWayGate);
        var room = InteractorTestHelpers.MakeRoom();
        room.TileMap.Setup(x => x.TileExists(new Point(2, 3))).Returns(false);
        var user = InteractorTestHelpers.MakeUser(new Point(2, 1), InteractorTestHelpers.MakePlayerDto(1));

        await interactor.OnTriggerAsync(room.Room.Object, item, user.User.Object);

        furniHelper.Verify(x => x.UpdateMetaDataForItemAsync(It.IsAny<IRoomLogic>(),
            It.IsAny<PlayerFurnitureItemPlacementDataDto>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task OnTriggerAsync_UserInFront_WalksThroughGate()
    {
        using var factory = new SqliteTestDbFactory();
        var (interactor, tileHelper, furniHelper) = MakeInteractor(factory);
        tileHelper.Setup(x => x.GetPointInFront(2, 2, HDirection.North, It.IsAny<int>())).Returns(new Point(2, 1));
        tileHelper.Setup(x => x.GetOppositeDirection(HDirection.North)).Returns(HDirection.South);
        tileHelper.Setup(x => x.GetPointInFront(2, 2, HDirection.South, It.IsAny<int>())).Returns(new Point(2, 3));

        var item = InteractorTestHelpers.MakeItem(100, 2, 2, HDirection.North, FurnitureItemInteractionType.OneWayGate);
        var room = InteractorTestHelpers.MakeRoom();
        room.TileMap.Setup(x => x.TileExists(new Point(2, 3))).Returns(true);
        var user = InteractorTestHelpers.MakeUser(new Point(2, 1), InteractorTestHelpers.MakePlayerDto(1));

        await interactor.OnTriggerAsync(room.Room.Object, item, user.User.Object);
        Assert.Multiple(() =>
        {
            Assert.That(user.Point, Is.EqualTo(new Point(2, 3)));
            Assert.That(user.User.Object.Direction, Is.EqualTo(HDirection.South));
            Assert.That(user.User.Object.DirectionHead, Is.EqualTo(HDirection.South));
            Assert.That(user.User.Object.NeedsUpdate, Is.True);
            Assert.That(user.User.Object.CanWalk, Is.True);
            Assert.That(user.User.Object.OverridePoints, Is.Empty);
        });
        furniHelper.Verify(x => x.UpdateMetaDataForItemAsync(room.Room.Object, item, "1"), Times.Once);
        furniHelper.Verify(x => x.UpdateMetaDataForItemAsync(room.Room.Object, item, "0"), Times.Once);
    }

    [Test]
    public async Task OnPlaceAsync_ResetsMetaDataAndPersists()
    {
        using var factory = new SqliteTestDbFactory();
        await InteractorTestHelpers.SeedItemRowAsync(factory, 100, "9");
        var (interactor, _, furniHelper) = MakeInteractor(factory);

        var item = InteractorTestHelpers.MakeItem(100, 2, 2, HDirection.North, FurnitureItemInteractionType.OneWayGate);
        var room = InteractorTestHelpers.MakeRoom();
        var user = InteractorTestHelpers.MakeUser(new Point(0, 0), InteractorTestHelpers.MakePlayerDto(1));

        await interactor.OnPlaceAsync(room.Room.Object, item, user.User.Object);

        furniHelper.Verify(x => x.UpdateMetaDataForItemAsync(room.Room.Object, item, "0"), Times.Once);
        await using var db = factory.CreateDbContext();
        Assert.That(db.PlayerFurnitureItems.Single(x => x.Id == 100).MetaData, Is.EqualTo("0"));
    }
}

[TestFixture]
public class GateInteractorTests
{
    private static (GateInteractor Interactor, Mock<IRoomFurnitureItemHelperService> FurniHelper) MakeInteractor(
        SqliteTestDbFactory factory)
    {
        var furniHelper = new Mock<IRoomFurnitureItemHelperService>();

        return (new GateInteractor(factory, furniHelper.Object), furniHelper);
    }

    [Test]
    public void InteractionTypes_ContainsGate()
    {
        using var factory = new SqliteTestDbFactory();

        Assert.That(MakeInteractor(factory).Interactor.InteractionTypes,
            Is.EqualTo(new[] { FurnitureItemInteractionType.Gate }));
    }

    [Test]
    public async Task OnTriggerAsync_UserOnGate_DoesNothing()
    {
        using var factory = new SqliteTestDbFactory();
        var (interactor, furniHelper) = MakeInteractor(factory);

        var item = InteractorTestHelpers.MakeItem(100, 3, 2, HDirection.North, FurnitureItemInteractionType.Gate);
        var room = InteractorTestHelpers.MakeRoom();
        room.TileMap.Setup(x => x.UsersAtPoint(new Point(3, 2))).Returns(true);
        var user = InteractorTestHelpers.MakeUser(new Point(0, 0), InteractorTestHelpers.MakePlayerDto(1));

        await interactor.OnTriggerAsync(room.Room.Object, item, user.User.Object);

        furniHelper.Verify(x => x.UpdateMetaDataForItemAsync(It.IsAny<IRoomLogic>(),
            It.IsAny<PlayerFurnitureItemPlacementDataDto>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task OnTriggerAsync_WalkerHeadingToGate_DoesNothing()
    {
        using var factory = new SqliteTestDbFactory();
        var (interactor, furniHelper) = MakeInteractor(factory);

        var item = InteractorTestHelpers.MakeItem(100, 3, 2, HDirection.North, FurnitureItemInteractionType.Gate);
        var room = InteractorTestHelpers.MakeRoom();
        room.TileMap.Setup(x => x.UsersAtPoint(new Point(3, 2))).Returns(false);

        var walker = new Mock<IRoomUser>();
        walker.SetupGet(x => x.IsWalking).Returns(true);
        walker.SetupGet(x => x.NextPoint).Returns(new Point(3, 2));
        room.UserRepository.Setup(x => x.GetAll()).Returns(new List<IRoomUser> { walker.Object });

        var user = InteractorTestHelpers.MakeUser(new Point(0, 0), InteractorTestHelpers.MakePlayerDto(1));

        await interactor.OnTriggerAsync(room.Room.Object, item, user.User.Object);

        furniHelper.Verify(x => x.UpdateMetaDataForItemAsync(It.IsAny<IRoomLogic>(),
            It.IsAny<PlayerFurnitureItemPlacementDataDto>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task OnTriggerAsync_ClosedGate_Opens()
    {
        using var factory = new SqliteTestDbFactory();
        var (interactor, furniHelper) = MakeInteractor(factory);

        var map = new short[8, 8];
        var item = InteractorTestHelpers.MakeItem(100, 3, 2, HDirection.North, FurnitureItemInteractionType.Gate);
        var room = InteractorTestHelpers.MakeRoom(map);
        var user = InteractorTestHelpers.MakeUser(new Point(0, 0), InteractorTestHelpers.MakePlayerDto(1));

        await interactor.OnTriggerAsync(room.Room.Object, item, user.User.Object);

        furniHelper.Verify(x => x.UpdateMetaDataForItemAsync(room.Room.Object, item, "1"), Times.Once);
        Assert.That(map[2, 3], Is.EqualTo((short)1));
    }

    [Test]
    public async Task OnTriggerAsync_OpenGate_Closes()
    {
        using var factory = new SqliteTestDbFactory();
        var (interactor, furniHelper) = MakeInteractor(factory);

        var map = new short[8, 8];
        map[2, 3] = 1;
        var item = InteractorTestHelpers.MakeItem(100, 3, 2, HDirection.North, FurnitureItemInteractionType.Gate, "1");
        var room = InteractorTestHelpers.MakeRoom(map);
        var user = InteractorTestHelpers.MakeUser(new Point(0, 0), InteractorTestHelpers.MakePlayerDto(1));

        await interactor.OnTriggerAsync(room.Room.Object, item, user.User.Object);

        furniHelper.Verify(x => x.UpdateMetaDataForItemAsync(room.Room.Object, item, "0"), Times.Once);
        Assert.That(map[2, 3], Is.EqualTo((short)0));
    }

    [Test]
    public async Task OnPlaceAsync_ResetsMetaDataAndPersists()
    {
        using var factory = new SqliteTestDbFactory();
        await InteractorTestHelpers.SeedItemRowAsync(factory, 100, "1");
        var (interactor, furniHelper) = MakeInteractor(factory);

        var item = InteractorTestHelpers.MakeItem(100, 3, 2, HDirection.North, FurnitureItemInteractionType.Gate);
        var room = InteractorTestHelpers.MakeRoom();
        var user = InteractorTestHelpers.MakeUser(new Point(0, 0), InteractorTestHelpers.MakePlayerDto(1));

        await interactor.OnPlaceAsync(room.Room.Object, item, user.User.Object);

        furniHelper.Verify(x => x.UpdateMetaDataForItemAsync(room.Room.Object, item, "0"), Times.Once);
        await using var db = factory.CreateDbContext();
        Assert.That(db.PlayerFurnitureItems.Single(x => x.Id == 100).MetaData, Is.EqualTo("0"));
    }
}

[TestFixture]
public class ClubGateInteractorTests
{
    private static (ClubGateInteractor Interactor, Mock<IRoomFurnitureItemHelperService> FurniHelper) MakeInteractor(
        SqliteTestDbFactory factory)
    {
        var furniHelper = new Mock<IRoomFurnitureItemHelperService>();

        return (new ClubGateInteractor(factory, furniHelper.Object), furniHelper);
    }

    private static PlayerDto MakeClubPlayer(long id, DateTimeOffset expiresAt, string subscriptionName = "HABBO_CLUB")
    {
        return InteractorTestHelpers.MakePlayerDto(id,
        [
            new PlayerSubscriptionDto
            {
                Id = 1,
                PlayerId = id,
                SubscriptionId = 1,
                Subscription = new SubscriptionDto { Id = 1, Name = subscriptionName },
                ExpiresAt = expiresAt
            }
        ]);
    }

    [Test]
    public void InteractionTypes_ContainsClubGate()
    {
        using var factory = new SqliteTestDbFactory();

        Assert.That(MakeInteractor(factory).Interactor.InteractionTypes,
            Is.EqualTo(new[] { FurnitureItemInteractionType.ClubGate }));
    }

    [Test]
    public async Task OnTriggerAsync_ExpiredClub_DoesNothing()
    {
        using var factory = new SqliteTestDbFactory();
        var (interactor, furniHelper) = MakeInteractor(factory);

        var item = InteractorTestHelpers.MakeItem(100, 3, 2, HDirection.North, FurnitureItemInteractionType.ClubGate);
        var room = InteractorTestHelpers.MakeRoom();
        var user = InteractorTestHelpers.MakeUser(new Point(0, 0),
            MakeClubPlayer(1, DateTimeOffset.Now.AddDays(-1)));

        await interactor.OnTriggerAsync(room.Room.Object, item, user.User.Object);

        furniHelper.Verify(x => x.UpdateMetaDataForItemAsync(It.IsAny<IRoomLogic>(),
            It.IsAny<PlayerFurnitureItemPlacementDataDto>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task OnTriggerAsync_OtherSubscription_DoesNothing()
    {
        using var factory = new SqliteTestDbFactory();
        var (interactor, furniHelper) = MakeInteractor(factory);

        var item = InteractorTestHelpers.MakeItem(100, 3, 2, HDirection.North, FurnitureItemInteractionType.ClubGate);
        var room = InteractorTestHelpers.MakeRoom();
        var user = InteractorTestHelpers.MakeUser(new Point(0, 0),
            MakeClubPlayer(1, DateTimeOffset.Now.AddDays(30), "OTHER_CLUB"));

        await interactor.OnTriggerAsync(room.Room.Object, item, user.User.Object);

        furniHelper.Verify(x => x.UpdateMetaDataForItemAsync(It.IsAny<IRoomLogic>(),
            It.IsAny<PlayerFurnitureItemPlacementDataDto>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task OnTriggerAsync_ActiveClub_TogglesGate()
    {
        using var factory = new SqliteTestDbFactory();
        var (interactor, furniHelper) = MakeInteractor(factory);

        var map = new short[8, 8];
        var item = InteractorTestHelpers.MakeItem(100, 3, 2, HDirection.North, FurnitureItemInteractionType.ClubGate);
        var room = InteractorTestHelpers.MakeRoom(map);
        var user = InteractorTestHelpers.MakeUser(new Point(0, 0),
            MakeClubPlayer(1, DateTimeOffset.Now.AddDays(30)));

        await interactor.OnTriggerAsync(room.Room.Object, item, user.User.Object);

        furniHelper.Verify(x => x.UpdateMetaDataForItemAsync(room.Room.Object, item, "1"), Times.Once);
        Assert.That(map[2, 3], Is.EqualTo((short)1));
    }
}

[TestFixture]
public class VendingInteractorTests
{
    private static (VendingInteractor Interactor, Mock<IRoomTileMapHelperService> TileHelper,
        Mock<IRoomFurnitureItemHelperService> FurniHelper) MakeInteractor()
    {
        var tileHelper = new Mock<IRoomTileMapHelperService>();
        var furniHelper = new Mock<IRoomFurnitureItemHelperService>();

        return (new VendingInteractor(tileHelper.Object, furniHelper.Object,
            new InlineRoomDeferralScheduler(),
            NullLogger<VendingInteractor>.Instance), tileHelper, furniHelper);
    }

    [Test]
    public void InteractionTypes_ContainsVendingMachine()
    {
        Assert.That(MakeInteractor().Interactor.InteractionTypes,
            Is.EqualTo(new[] { FurnitureItemInteractionType.VendingMachine }));
    }

    [Test]
    public async Task OnTriggerAsync_UserInFront_GivesHandItem()
    {
        var (interactor, tileHelper, _) = MakeInteractor();
        tileHelper.Setup(x => x.GetOppositeDirection(HDirection.East)).Returns(HDirection.West);
        tileHelper.Setup(x => x.GetPointInFront(2, 2, HDirection.East, It.IsAny<int>())).Returns(new Point(3, 2));

        var item = InteractorTestHelpers.MakeItem(100, 2, 2, HDirection.East,
            FurnitureItemInteractionType.VendingMachine, handItems: [new HandItemDto { Id = 7, Name = "coffee" }]);
        var room = InteractorTestHelpers.MakeRoom();
        var user = InteractorTestHelpers.MakeUser(new Point(3, 2), InteractorTestHelpers.MakePlayerDto(5));

        await interactor.OnTriggerAsync(room.Room.Object, item, user.User.Object);
        Assert.Multiple(() =>
        {
            Assert.That(user.User.Object.Direction, Is.EqualTo(HDirection.West));
            Assert.That(user.User.Object.DirectionHead, Is.EqualTo(HDirection.West));
            Assert.That(user.User.Object.NeedsUpdate, Is.True);
            Assert.That(user.User.Object.HandItemId, Is.EqualTo(7));
            Assert.That(user.User.Object.HandItemSet, Is.Not.EqualTo(default(DateTime)));
            Assert.That(room.Broadcasts, Has.Count.EqualTo(1));
        });
        var writer = (RoomUserHandItemWriter)room.Broadcasts[0];
        Assert.Multiple(() =>
        {
            Assert.That(writer.UserId, Is.EqualTo(5));
            Assert.That(writer.ItemId, Is.EqualTo(7));
        });
    }

    [Test]
    public async Task OnTriggerAsync_UserAway_WalksToFrontThenVends()
    {
        var (interactor, tileHelper, furniHelper) = MakeInteractor();
        tileHelper.Setup(x => x.GetOppositeDirection(HDirection.East)).Returns(HDirection.West);
        tileHelper.Setup(x => x.GetPointInFront(2, 2, HDirection.East, It.IsAny<int>())).Returns(new Point(3, 2));

        var item = InteractorTestHelpers.MakeItem(100, 2, 2, HDirection.East,
            FurnitureItemInteractionType.VendingMachine, handItems: [new HandItemDto { Id = 7, Name = "coffee" }]);
        var room = InteractorTestHelpers.MakeRoom();
        var user = InteractorTestHelpers.MakeUser(new Point(0, 0), InteractorTestHelpers.MakePlayerDto(5));

        await interactor.OnTriggerAsync(room.Room.Object, item, user.User.Object);

        for (var i = 0; i < 100 && room.Broadcasts.Count == 0; i++)
        {
            await Task.Delay(50);
        }

        Assert.Multiple(() =>
        {
            Assert.That(user.Point, Is.EqualTo(new Point(3, 2)));
            Assert.That(user.User.Object.HandItemId, Is.EqualTo(7));
            Assert.That(room.Broadcasts, Has.Count.EqualTo(1));
        });
        user.User.Verify(x => x.WalkToPoint(new Point(3, 2), It.IsAny<Action?>()), Times.Once);
        furniHelper.Verify(x => x.UpdateMetaDataForItemAsync(room.Room.Object, item, "1"), Times.Once);
        furniHelper.Verify(x => x.UpdateMetaDataForItemAsync(room.Room.Object, item, "0"), Times.Once);
    }
}

[TestFixture]
public class DiceInteractorTests
{
    [Test]
    public void InteractionTypes_ContainsDice()
    {
        var interactor = new DiceInteractor(Mock.Of<IRoomFurnitureItemHelperService>(), NullLogger<DiceInteractor>.Instance);

        Assert.That(interactor.InteractionTypes, Is.EqualTo(new[] { "dice" }));
    }

    [Test]
    public async Task OnTriggerAsync_SpinsThenRollsBetween1And6()
    {
        var values = new List<string>();
        var rolled = new TaskCompletionSource();
        var furniHelper = new Mock<IRoomFurnitureItemHelperService>();
        furniHelper.Setup(x => x.UpdateMetaDataForItemAsync(It.IsAny<IRoomLogic>(),
                It.IsAny<PlayerFurnitureItemPlacementDataDto>(), It.IsAny<string>()))
            .Callback<IRoomLogic, PlayerFurnitureItemPlacementDataDto, string>((_, _, metaData) =>
            {
                lock (values)
                {
                    values.Add(metaData);

                    if (values.Count == 2)
                    {
                        rolled.TrySetResult();
                    }
                }
            })
            .Returns(Task.CompletedTask);

        var interactor = new DiceInteractor(furniHelper.Object, NullLogger<DiceInteractor>.Instance);
        var item = InteractorTestHelpers.MakeItem(100, 2, 2, HDirection.North, "dice");
        var room = InteractorTestHelpers.MakeRoom();
        var user = InteractorTestHelpers.MakeUser(new Point(0, 0), InteractorTestHelpers.MakePlayerDto(1));

        await interactor.OnTriggerAsync(room.Room.Object, item, user.User.Object);

        Assert.That(values[0], Is.EqualTo("-1"));
        await rolled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(int.Parse(values[1]), Is.InRange(1, 6));
        room.Room.Verify(x => x.RunLockedAsync(It.IsAny<Func<Task>>()), Times.Once);
    }
}
