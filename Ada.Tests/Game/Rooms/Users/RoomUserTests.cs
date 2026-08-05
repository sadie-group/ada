using System.Drawing;
using Ada.API;
using Ada.API.DTOs.Furniture;
using Ada.API.DTOs.Players;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Networking;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Enums.Game.Rooms;
using Ada.Core.Enums.Game.Rooms.Users;
using Ada.Core.Enums.Miscellaneous;
using Ada.Db.Models.Constants;
using Ada.Game.Rooms.Mapping;
using Ada.Game.Rooms.PathFinding;
using Ada.Game.Rooms.Users;
using Ada.Networking.Writers.Rooms.Users;
using Ada.Networking.Writers.Rooms.Users.HandItems;
using Moq;

namespace Ada.Tests.Game.Rooms.Users;

[TestFixture]
public class RoomUserTests
{
    private sealed record Harness(
        RoomUser User,
        List<AbstractPacketWriter> Broadcasts,
        RoomTileMap TileMap,
        Mock<IRoomLogic> Room,
        Mock<IRoomWiredService> Wired,
        Mock<IRoomFurnitureItemInteractorRepository> Interactors,
        Mock<INetworkObject> NetworkObject,
        Mock<IRoomHelperService> RoomHelper);

    private static PlayerDto CreatePlayerDto() => new(
        1L,
        "TestUser",
        "test@example.com",
        DateTimeOffset.UtcNow,
        [],
        new PlayerDataDto(),
        new PlayerAvatarDataDto(),
        [],
        [],
        [],
        [],
        new PlayerNavigatorSettingsDto(),
        new PlayerGameSettingsDto(),
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
        [],
        [],
        [],
        [],
        []);

    private static PlayerFurnitureItemPlacementDataDto ItemAt(int id, int x, int y, string interactionType) => new()
    {
        Id = id,
        PlayerFurnitureItem = new PlayerFurnitureItemDto
        {
            FurnitureItemId = 0,
            LimitedData = "",
            MetaData = "",
            FurnitureItem = new FurnitureItemDto
            {
                Name = "",
                AssetName = "",
                InteractionType = interactionType,
                CanWalk = true
            }
        },
        PositionX = x,
        PositionY = y,
        Direction = HDirection.North
    };

    private static Harness Create(
        List<PlayerFurnitureItemPlacementDataDto>? furniture = null,
        Point? point = null,
        RoomControllerLevel controllerLevel = RoomControllerLevel.None,
        int secondsTillIdle = 3600,
        HDirection direction = HDirection.North,
        HDirection directionHead = HDirection.North)
    {
        furniture ??= [];
        var startPoint = point ?? new Point(0, 0);

        var roomDto = new RoomDto
        {
            FurnitureItems = [..furniture],
            Settings = new RoomSettingsDto()
        };

        var tileMap = new RoomTileMap("000\n000\n000", furniture);
        var broadcasts = new List<AbstractPacketWriter>();

        var room = new Mock<IRoomLogic>();
        room.SetupGet(x => x.Room).Returns(roomDto);
        room.SetupGet(x => x.TileMap).Returns(tileMap);
        room.Setup(x => x.BroadcastDataAsync(
                It.IsAny<AbstractPacketWriter>(),
                It.IsAny<IReadOnlyCollection<long>?>()))
            .Callback<AbstractPacketWriter, IReadOnlyCollection<long>?>((w, _) => broadcasts.Add(w))
            .Returns(Task.CompletedTask);

        var player = new Mock<IPlayerLogic>();
        player.SetupGet(x => x.Player).Returns(CreatePlayerDto());

        var wired = new Mock<IRoomWiredService>();

        wired.Setup(x => x.HasTriggers(
                It.IsAny<string>(),
                It.IsAny<ICollection<PlayerFurnitureItemPlacementDataDto>>()))
            .Returns(true);

        var interactors = new Mock<IRoomFurnitureItemInteractorRepository>();
        var networkObject = new Mock<INetworkObject>();
        var roomHelper = new Mock<IRoomHelperService>();

        var user = new RoomUser(
            room.Object,
            networkObject.Object,
            startPoint,
            0,
            directionHead,
            direction,
            player.Object,
            new ServerRoomConstants { SecondsTillUserIdle = secondsTillIdle },
            controllerLevel,
            tileMap,
            roomHelper.Object,
            wired.Object,
            new RoomPathFinderHelperService(),
            interactors.Object);

        tileMap.AddUnitToMap(startPoint, user);

        return new Harness(user, broadcasts, tileMap, room, wired, interactors, networkObject, roomHelper);
    }

    [Test]
    public void LookAtPoint_Standing_TurnsBodyAndHead()
    {
        var h = Create(point: new Point(1, 1), direction: HDirection.South, directionHead: HDirection.South);

        h.User.LookAtPoint(new Point(2, 2));

        Assert.That(h.User.Direction, Is.EqualTo(HDirection.SouthEast));
        Assert.That(h.User.DirectionHead, Is.EqualTo(HDirection.SouthEast));
    }

    [Test]
    public void LookAtPoint_SittingFacingAway_KeepsBodyAndHead()
    {
        var h = Create(point: new Point(1, 1));
        h.User.AddStatus(RoomUserStatus.Sit, "1.0");

        h.User.LookAtPoint(new Point(2, 2));

        Assert.That(h.User.Direction, Is.EqualTo(HDirection.North));
        Assert.That(h.User.DirectionHead, Is.EqualTo(HDirection.North));
    }

    [Test]
    public void LookAtPoint_SittingFacingNearby_TurnsHeadOnly()
    {
        var h = Create(point: new Point(1, 1), directionHead: HDirection.East);
        h.User.AddStatus(RoomUserStatus.Sit, "1.0");

        h.User.LookAtPoint(new Point(1, 0));

        Assert.That(h.User.Direction, Is.EqualTo(HDirection.North));
        Assert.That(h.User.DirectionHead, Is.EqualTo(HDirection.North));
    }

    [Test]
    public void ApplyFlatCtrlStatus_AddsControllerLevelStatus()
    {
        var h = Create(controllerLevel: RoomControllerLevel.Owner);

        h.User.ApplyFlatCtrlStatus();

        Assert.That(h.User.StatusMap[RoomUserStatus.FlatCtrl], Is.EqualTo("4"));
        Assert.That(h.User.NeedsUpdate, Is.True);
    }

    [TestCase(RoomControllerLevel.None, false)]
    [TestCase(RoomControllerLevel.Rights, true)]
    [TestCase(RoomControllerLevel.GuildRights, false)]
    [TestCase(RoomControllerLevel.GuildAdmin, false)]
    [TestCase(RoomControllerLevel.Owner, true)]
    [TestCase(RoomControllerLevel.Moderator, false)]
    public void HasRights_ReflectsControllerLevel(RoomControllerLevel level, bool expected)
    {
        var h = Create(controllerLevel: level);

        Assert.That(h.User.HasRights(), Is.EqualTo(expected));
    }

    [Test]
    public async Task RunPeriodicCheckAsync_HandItemExpired_ClearsAndBroadcasts()
    {
        var h = Create();
        h.User.HandItemId = 5;
        h.User.HandItemSet = DateTime.Now.AddSeconds(-31);

        await h.User.RunPeriodicCheckAsync();

        Assert.That(h.User.HandItemId, Is.Zero);
        var writer = h.Broadcasts.OfType<RoomUserHandItemWriter>().Single();
        Assert.That(writer.UserId, Is.EqualTo(1L));
        Assert.That(writer.ItemId, Is.Zero);
    }

    [Test]
    public async Task RunPeriodicCheckAsync_HandItemFresh_Kept()
    {
        var h = Create();
        h.User.HandItemId = 5;
        h.User.HandItemSet = DateTime.Now;

        await h.User.RunPeriodicCheckAsync();

        Assert.That(h.User.HandItemId, Is.EqualTo(5));
        Assert.That(h.Broadcasts.OfType<RoomUserHandItemWriter>(), Is.Empty);
    }

    [Test]
    public async Task RunPeriodicCheckAsync_SignExpired_Removed()
    {
        var h = Create();
        h.User.AddStatus(RoomUserStatus.Sign, "7");
        h.User.SignSet = DateTime.Now.AddSeconds(-6);

        await h.User.RunPeriodicCheckAsync();

        Assert.That(h.User.StatusMap.ContainsKey(RoomUserStatus.Sign), Is.False);
    }

    [Test]
    public async Task RunPeriodicCheckAsync_SignFresh_Kept()
    {
        var h = Create();
        h.User.AddStatus(RoomUserStatus.Sign, "7");
        h.User.SignSet = DateTime.Now;

        await h.User.RunPeriodicCheckAsync();

        Assert.That(h.User.StatusMap.ContainsKey(RoomUserStatus.Sign), Is.True);
    }

    [Test]
    public async Task RunPeriodicCheckAsync_IdleTransitions_BroadcastOnChangeOnly()
    {
        var h = Create(secondsTillIdle: 0);
        h.User.LastAction = DateTime.Now.AddSeconds(-5);

        await h.User.RunPeriodicCheckAsync();

        Assert.That(h.User.IsIdle, Is.True);

        h.User.LastAction = DateTime.Now.AddMinutes(5);

        await h.User.RunPeriodicCheckAsync();
        await h.User.RunPeriodicCheckAsync();

        Assert.That(h.User.IsIdle, Is.False);
        var idleWriters = h.Broadcasts.OfType<RoomUserIdleWriter>().ToList();
        Assert.That(idleWriters, Has.Count.EqualTo(2));
        Assert.That(idleWriters[0].IsIdle, Is.True);
        Assert.That(idleWriters[1].IsIdle, Is.False);
    }

    [Test]
    public async Task RunPeriodicCheckAsync_PendingStep_MovesUserAndRunsStepHooks()
    {
        var oldItem = ItemAt(1, 0, 0, "custom_step");
        var newItem = ItemAt(2, 1, 1, "custom_step");
        var trigger = ItemAt(99, 2, 2, FurnitureItemInteractionType.WiredTriggerUserWalksOnFurniture);
        var h = Create([oldItem, newItem]);

        var interactor = new Mock<IRoomFurnitureItemInteractor>();
        h.Interactors.Setup(x => x.GetInteractorsForType("custom_step"))
            .Returns(new List<IRoomFurnitureItemInteractor> { interactor.Object });
        h.Wired.Setup(x => x.GetTriggers(
                FurnitureItemInteractionType.WiredTriggerUserWalksOnFurniture,
                It.IsAny<IEnumerable<PlayerFurnitureItemPlacementDataDto>>(),
                "",
                It.Is<List<int>>(ids => ids.Contains(2))))
            .Returns(new[] { trigger });

        h.User.NextPoint = new Point(1, 1);

        await h.User.RunPeriodicCheckAsync();

        Assert.That(h.User.Point, Is.EqualTo(new Point(1, 1)));
        interactor.Verify(x => x.OnWalkedOffAsync(h.Room.Object, oldItem, h.User), Times.Once);
        interactor.Verify(x => x.OnWalkedOnAsync(h.Room.Object, newItem, h.User), Times.Once);
        h.Wired.Verify(x => x.RunTriggerForRoomAsync(h.Room.Object, trigger, h.User), Times.Once);
    }

    [Test]
    public void RunPeriodicCheckAsync_StepInteractorThrows_IsSwallowed()
    {
        var newItem = ItemAt(2, 1, 1, "custom_step");
        var h = Create([newItem]);

        var interactor = new Mock<IRoomFurnitureItemInteractor>();
        interactor.Setup(x => x.OnWalkedOnAsync(
                It.IsAny<IRoomLogic>(),
                It.IsAny<PlayerFurnitureItemPlacementDataDto>(),
                It.IsAny<IRoomUser>()))
            .ThrowsAsync(new InvalidOperationException());
        h.Interactors.Setup(x => x.GetInteractorsForType("custom_step"))
            .Returns(new List<IRoomFurnitureItemInteractor> { interactor.Object });

        h.User.NextPoint = new Point(1, 1);

        Assert.DoesNotThrowAsync(() => h.User.RunPeriodicCheckAsync());
        interactor.Verify(x => x.OnWalkedOnAsync(h.Room.Object, newItem, h.User), Times.Once);
    }

    [Test]
    public async Task RunPeriodicCheckAsync_EmptyInteractionType_SkipsInteractors()
    {
        var newItem = ItemAt(2, 1, 1, "");
        var h = Create([newItem]);

        h.User.NextPoint = new Point(1, 1);

        await h.User.RunPeriodicCheckAsync();

        Assert.That(h.User.Point, Is.EqualTo(new Point(1, 1)));
        h.Interactors.Verify(x => x.GetInteractorsForType(It.IsAny<string>()), Times.Never);
        h.Wired.Verify(x => x.GetTriggers(
            FurnitureItemInteractionType.WiredTriggerUserWalksOnFurniture,
            It.IsAny<IEnumerable<PlayerFurnitureItemPlacementDataDto>>(),
            "",
            It.IsAny<List<int>>()), Times.Once);
    }

    [Test]
    public async Task SendWhisperAsync_WritesWhisperToOwnStream()
    {
        var h = Create();
        var writers = new List<AbstractPacketWriter>();
        h.NetworkObject.Setup(x => x.WriteToStreamAsync(It.IsAny<AbstractPacketWriter>()))
            .Callback<AbstractPacketWriter>(writers.Add)
            .Returns(Task.CompletedTask);
        h.RoomHelper.Setup(x => x.GetEmotionFromMessage("hello")).Returns(RoomUserEmotion.Smile);

        await h.User.SendWhisperAsync("hello");

        var whisper = (RoomUserWhisperWriter)writers.Single();
        Assert.Multiple(() =>
        {
            Assert.That(whisper.SenderId, Is.EqualTo(1L));
            Assert.That(whisper.Message, Is.EqualTo("hello"));
            Assert.That(whisper.EmotionId, Is.EqualTo((int)RoomUserEmotion.Smile));
            Assert.That(whisper.MessageLength, Is.EqualTo(5));
        });
    }

    [Test]
    public async Task SetEffectAsync_SetsActiveEffectAndBroadcasts()
    {
        var h = Create();

        await h.User.SetEffectAsync(RoomUserEffect.Swimming);

        Assert.That(h.User.ActiveEffectId, Is.EqualTo((int)RoomUserEffect.Swimming));
        var writer = h.Broadcasts.OfType<RoomUserEffectWriter>().Single();
        Assert.That(writer.EffectId, Is.EqualTo((int)RoomUserEffect.Swimming));
    }

    [Test]
    public async Task DisposeAsync_RemovesUserFromUnitMap()
    {
        var h = Create();

        await h.User.DisposeAsync();

        Assert.That(h.TileMap.UnitMap[new Point(0, 0)], Does.Not.Contain(h.User));
    }

    [Test]
    public void DisposeAsync_NotOnMap_DoesNotThrow()
    {
        var h = Create();
        h.TileMap.UnitMap.Clear();

        Assert.DoesNotThrowAsync(() => h.User.DisposeAsync().AsTask());
    }
}
