using System.Drawing;
using Ada.API;
using Ada.API.DTOs;
using Ada.API.DTOs.Players;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Networking;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Enums.Miscellaneous;
using Ada.Game.Rooms.Mapping;
using Ada.Game.Rooms.Wired.Conditions;
using Ada.Game.Rooms.Wired.Effects;
using Ada.Networking.Writers.Rooms.Users;
using Moq;

namespace Ada.Tests.Game.Rooms.Wired;

public class WiredEffectStrategyTests : MockHelpers
{
    private static PlayerFurnitureItemPlacementDataDto MockWiredItem(
        string interactionType,
        string intParameters = "",
        List<PlayerFurnitureItemPlacementDataDto>? selectedItems = null,
        string message = "")
    {
        var item = MockFurnitureItemPlacementData(interactionType);

        item.WiredData = new PlayerFurnitureItemWiredDataDto
        {
            PlayerFurnitureItemPlacementDataId = item.Id,
            PlacementData = item,
            Message = message,
            IntParameters = intParameters,
            SelectedItems = selectedItems ?? []
        };

        return item;
    }

    private static (Mock<IRoomLogic> Room, Mock<IRoomUserRepository> Users) MockRoom(
        List<PlayerFurnitureItemPlacementDataDto> furnitureItems,
        List<IRoomUser>? users = null,
        long ownerId = 0)
    {
        var roomDto = new RoomDto
        {
            OwnerId = ownerId,
            FurnitureItems = [..furnitureItems]
        };

        var room = new Mock<IRoomLogic>();
        room.SetupGet(x => x.Room).Returns(roomDto);

        var userRepository = new Mock<IRoomUserRepository>();
        userRepository.Setup(x => x.GetAll()).Returns(users ?? []);
        room.SetupGet(x => x.UserRepository).Returns(userRepository.Object);
        room.SetupGet(x => x.TileMap).Returns(new RoomTileMap("x", furnitureItems));

        return (room, userRepository);
    }

    private static Mock<IRoomUser> MockUser(
        long id = 1,
        ICollection<PlayerBadgeDto>? badges = null,
        Point? point = null)
    {
        var playerData = new PlayerDto(
            id,
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
            badges ?? [],
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

        var player = new Mock<IPlayerLogic>();
        player.SetupGet(x => x.Player).Returns(playerData);

        var user = new Mock<IRoomUser>();
        user.SetupGet(x => x.Player).Returns(player.Object);
        user.SetupGet(x => x.Point).Returns(point ?? new Point(0, 0));

        var network = new Mock<INetworkObject>();
        user.SetupGet(x => x.NetworkObject).Returns(network.Object);

        return user;
    }

    private static (WiredEffectMoveToDirectionStrategy Strategy,
        Mock<IRoomTileMapHelperService> TileHelper,
        Mock<IRoomFurnitureItemHelperService> FurniHelper) CreateMoveToDirectionStrategy()
    {
        var tileHelper = new Mock<IRoomTileMapHelperService>();
        var furniHelper = new Mock<IRoomFurnitureItemHelperService>();
        return (new WiredEffectMoveToDirectionStrategy(tileHelper.Object, furniHelper.Object), tileHelper, furniHelper);
    }

    [Test]
    public void InteractionTypes_MatchConstants()
    {
        var tileHelper = new Mock<IRoomTileMapHelperService>();
        var furniHelper = new Mock<IRoomFurnitureItemHelperService>();

        Assert.Multiple(() =>
        {
            Assert.That(new WiredEffectToggleFurnitureStateStrategy(furniHelper.Object).InteractionType,
                Is.EqualTo(FurnitureItemInteractionType.WiredEffectToggleFurnitureState));
            Assert.That(new WiredEffectShowMessageStrategy().InteractionType,
                Is.EqualTo(FurnitureItemInteractionType.WiredEffectShowMessage));
            Assert.That(new WiredEffectMuteTriggererStrategy(Mock.Of<IRoomFloodProtectionService>()).InteractionType,
                Is.EqualTo(FurnitureItemInteractionType.WiredEffectMuteTriggerer));
            Assert.That(new WiredEffectKickUserStrategy().InteractionType,
                Is.EqualTo(FurnitureItemInteractionType.WiredEffectKickUser));
            Assert.That(new WiredEffectTeleportUserStrategy().InteractionType,
                Is.EqualTo(FurnitureItemInteractionType.WiredEffectTeleportToFurniture));
            Assert.That(new WiredEffectCallAnotherStackStrategy(Mock.Of<IServiceProvider>()).InteractionType,
                Is.EqualTo(FurnitureItemInteractionType.WiredEffectCallAnotherStack));
            Assert.That(new WiredEffectMoveToDirectionStrategy(tileHelper.Object, furniHelper.Object).InteractionType,
                Is.EqualTo(FurnitureItemInteractionType.WiredEffectChangeFurnitureDirection));
            Assert.That(new WiredEffectFleeStrategy(tileHelper.Object, furniHelper.Object).InteractionType,
                Is.EqualTo(FurnitureItemInteractionType.WiredEffectFleeFromClosestUser));
            Assert.That(new WiredEffectMoveRotateStrategy(tileHelper.Object, furniHelper.Object).InteractionType,
                Is.EqualTo(FurnitureItemInteractionType.WiredEffectMoveRotateFurniture));
            Assert.That(new WiredConditionFurnitureHasFurnitureStrategy().InteractionType,
                Is.EqualTo(FurnitureItemInteractionType.WiredConditionFurnitureHasFurniture));
            Assert.That(new WiredConditionNotFurnitureHasFurnitureStrategy().InteractionType,
                Is.EqualTo(FurnitureItemInteractionType.WiredConditionNotFurnitureHasFurniture));
            Assert.That(new WiredConditionTriggererWearsBadgeStrategy().InteractionType,
                Is.EqualTo(FurnitureItemInteractionType.WiredConditionTriggererWearsBadge));
            Assert.That(new WiredConditionNotTriggererWearsBadgeStrategy().InteractionType,
                Is.EqualTo(FurnitureItemInteractionType.WiredConditionNotTriggererWearsBadge));
        });
    }

    [Test]
    public async Task ToggleFurnitureState_NullWiredData_DoesNothing()
    {
        var furniHelper = new Mock<IRoomFurnitureItemHelperService>();
        var strategy = new WiredEffectToggleFurnitureStateStrategy(furniHelper.Object);
        var effect = MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredEffectToggleFurnitureState);
        var (room, _) = MockRoom([effect]);

        await strategy.ExecuteAsync(room.Object, effect, null);

        furniHelper.Verify(
            x => x.CycleInteractionStateForItemAsync(It.IsAny<IRoomLogic>(), It.IsAny<PlayerFurnitureItemPlacementDataDto>()),
            Times.Never);
    }

    [Test]
    public async Task ToggleFurnitureState_CyclesOnlyItemsPresentInRoom()
    {
        var furniHelper = new Mock<IRoomFurnitureItemHelperService>();
        var strategy = new WiredEffectToggleFurnitureStateStrategy(furniHelper.Object);

        var present = MockFurnitureItemPlacementData("lamp", id: 7);
        var missing = MockFurnitureItemPlacementData("lamp", id: 8);
        var effect = MockWiredItem(
            FurnitureItemInteractionType.WiredEffectToggleFurnitureState,
            selectedItems: [present, missing]);
        var (room, _) = MockRoom([effect, present]);

        await strategy.ExecuteAsync(room.Object, effect, null);

        furniHelper.Verify(x => x.CycleInteractionStateForItemAsync(room.Object, present), Times.Once);
        furniHelper.Verify(
            x => x.CycleInteractionStateForItemAsync(It.IsAny<IRoomLogic>(), It.IsAny<PlayerFurnitureItemPlacementDataDto>()),
            Times.Once);
    }

    [Test]
    public async Task ShowMessage_NoUserOrEmptyMessage_DoesNothing()
    {
        var strategy = new WiredEffectShowMessageStrategy();
        var user = MockUser();
        var (room, _) = MockRoom([]);

        await strategy.ExecuteAsync(room.Object, MockWiredItem("x", message: "hello"), null);
        await strategy.ExecuteAsync(room.Object, MockWiredItem("x"), user.Object);

        Mock.Get(user.Object.NetworkObject)
            .Verify(x => x.WriteToStreamAsync(It.IsAny<AbstractPacketWriter>()), Times.Never);
    }

    [Test]
    public async Task ShowMessage_WhispersToTriggerer()
    {
        var strategy = new WiredEffectShowMessageStrategy();
        var user = MockUser(id: 42);
        var (room, _) = MockRoom([]);

        await strategy.ExecuteAsync(room.Object, MockWiredItem("x", message: "hello"), user.Object);

        Mock.Get(user.Object.NetworkObject).Verify(x => x.WriteToStreamAsync(
                It.Is<AbstractPacketWriter>(w =>
                    ((RoomUserWhisperWriter)w).SenderId == 42 &&
                    ((RoomUserWhisperWriter)w).Message == "hello" &&
                    ((RoomUserWhisperWriter)w).MessageLength == 5 &&
                    ((RoomUserWhisperWriter)w).ChatBubbleId == (int)ChatBubble.Alert)),
            Times.Once);
    }

    [Test]
    public async Task MuteTriggerer_NullUserOrOwner_DoesNothing()
    {
        var floodProtection = new Mock<IRoomFloodProtectionService>();
        var strategy = new WiredEffectMuteTriggererStrategy(floodProtection.Object);
        var owner = MockUser(id: 9);
        var (room, _) = MockRoom([], ownerId: 9);

        await strategy.ExecuteAsync(room.Object, MockWiredItem("x"), null);
        await strategy.ExecuteAsync(room.Object, MockWiredItem("x"), owner.Object);

        floodProtection.Verify(x => x.MuteFor(It.IsAny<long>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task MuteTriggerer_MutesForConfiguredMinutes()
    {
        var floodProtection = new Mock<IRoomFloodProtectionService>();
        var strategy = new WiredEffectMuteTriggererStrategy(floodProtection.Object);
        var user = MockUser(id: 3);
        var (room, _) = MockRoom([]);

        await strategy.ExecuteAsync(room.Object, MockWiredItem("x", intParameters: "5"), user.Object);

        floodProtection.Verify(x => x.MuteFor(3, 300), Times.Once);
        user.Verify(x => x.SendWhisperAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task MuteTriggerer_DefaultsToOneMinuteAndWhispers()
    {
        var floodProtection = new Mock<IRoomFloodProtectionService>();
        var strategy = new WiredEffectMuteTriggererStrategy(floodProtection.Object);
        var user = MockUser(id: 3);
        var (room, _) = MockRoom([]);

        await strategy.ExecuteAsync(room.Object, MockWiredItem("x", message: "muted!"), user.Object);

        floodProtection.Verify(x => x.MuteFor(3, 60), Times.Once);
        user.Verify(x => x.SendWhisperAsync("muted!"), Times.Once);
    }

    [Test]
    public async Task KickUser_NullUserOrOwner_DoesNothing()
    {
        var strategy = new WiredEffectKickUserStrategy();
        var owner = MockUser(id: 9);
        var (room, users) = MockRoom([], ownerId: 9);

        await strategy.ExecuteAsync(room.Object, MockWiredItem("x"), null);
        await strategy.ExecuteAsync(room.Object, MockWiredItem("x"), owner.Object);

        users.Verify(x => x.TryRemoveAsync(It.IsAny<long>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Test]
    public async Task KickUser_RemovesUserAndAlerts()
    {
        var strategy = new WiredEffectKickUserStrategy();
        var user = MockUser(id: 5);
        var (room, users) = MockRoom([]);

        await strategy.ExecuteAsync(room.Object, MockWiredItem("x", message: "bye"), user.Object);

        users.Verify(x => x.TryRemoveAsync(5, true, true), Times.Once);
        Mock.Get(user.Object.Player).Verify(x => x.SendAlertAsync("bye"), Times.Once);
    }

    [Test]
    public async Task KickUser_NoMessage_RemovesWithoutAlert()
    {
        var strategy = new WiredEffectKickUserStrategy();
        var user = MockUser(id: 5);
        var (room, users) = MockRoom([]);

        await strategy.ExecuteAsync(room.Object, MockWiredItem("x"), user.Object);

        users.Verify(x => x.TryRemoveAsync(5, true, true), Times.Once);
        Mock.Get(user.Object.Player).Verify(x => x.SendAlertAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task TeleportUser_NoUserOrNoSelection_DoesNothing()
    {
        var strategy = new WiredEffectTeleportUserStrategy();
        var user = MockUser();
        var (room, _) = MockRoom([]);

        await strategy.ExecuteAsync(room.Object, MockWiredItem("x", selectedItems: [MockFurnitureItemPlacementData("t", id: 2)]), null);
        await strategy.ExecuteAsync(room.Object, MockWiredItem("x"), user.Object);

        user.Verify(x => x.SetPositionAsync(It.IsAny<Point>()), Times.Never);
    }

    [Test]
    public async Task TeleportUser_MovesTriggererToSelectedItem()
    {
        var strategy = new WiredEffectTeleportUserStrategy();
        var user = MockUser();
        var target = MockFurnitureItemPlacementData("t", 3, 4, id: 2);
        var (room, _) = MockRoom([target]);

        await strategy.ExecuteAsync(room.Object, MockWiredItem("x", selectedItems: [target]), user.Object);

        user.Verify(x => x.SetPositionAsync(new Point(3, 4)), Times.Once);
    }

    [Test]
    public async Task TeleportUser_SelectedItemMissingFromRoom_DoesNothing()
    {
        var strategy = new WiredEffectTeleportUserStrategy();
        var user = MockUser();
        var target = MockFurnitureItemPlacementData("t", 3, 4, id: 2);
        var (room, _) = MockRoom([]);

        await strategy.ExecuteAsync(room.Object, MockWiredItem("x", selectedItems: [target]), user.Object);

        user.Verify(x => x.SetPositionAsync(It.IsAny<Point>()), Times.Never);
    }

    [Test]
    public async Task CallAnotherStack_NoSelection_DoesNotResolveService()
    {
        var serviceProvider = new Mock<IServiceProvider>();
        var strategy = new WiredEffectCallAnotherStackStrategy(serviceProvider.Object);
        var (room, _) = MockRoom([]);

        await strategy.ExecuteAsync(room.Object, MockWiredItem("x"), null);

        serviceProvider.Verify(x => x.GetService(It.IsAny<Type>()), Times.Never);
    }

    [Test]
    public async Task CallAnotherStack_RunsSelectedTriggersOnly()
    {
        var wiredService = new Mock<IRoomWiredService>();
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IRoomWiredService))).Returns(wiredService.Object);

        var strategy = new WiredEffectCallAnotherStackStrategy(serviceProvider.Object);

        var trigger = MockFurnitureItemPlacementData("wf_trg_says", id: 1);
        var notTrigger = MockFurnitureItemPlacementData("wf_act_show", id: 2);
        var missing = MockFurnitureItemPlacementData("wf_trg_says", id: 3);
        var user = MockUser();

        var (room, _) = MockRoom([trigger, notTrigger]);

        await strategy.ExecuteAsync(
            room.Object,
            MockWiredItem("x", selectedItems: [trigger, notTrigger, missing]),
            user.Object);

        wiredService.Verify(x => x.RunTriggerForRoomAsync(room.Object, trigger, user.Object), Times.Once);
        wiredService.Verify(
            x => x.RunTriggerForRoomAsync(It.IsAny<IRoomLogic>(), It.IsAny<PlayerFurnitureItemPlacementDataDto>(), It.IsAny<IRoomUser?>()),
            Times.Once);
    }

    [Test]
    public async Task MovementEffect_NullWiredDataOrMissingItem_DoesNothing()
    {
        var (strategy, tileHelper, furniHelper) = CreateMoveToDirectionStrategy();

        var missing = MockFurnitureItemPlacementData("lamp", id: 4);
        var (room, _) = MockRoom([]);

        await strategy.ExecuteAsync(room.Object, MockFurnitureItemPlacementData("x"), null);
        await strategy.ExecuteAsync(room.Object, MockWiredItem("x", selectedItems: [missing]), null);

        tileHelper.Verify(
            x => x.CanPlaceAt(It.IsAny<IEnumerable<Point>>(), It.IsAny<IRoomTileMap>(),
                It.IsAny<ICollection<PlayerFurnitureItemPlacementDataDto>>(), It.IsAny<bool>()),
            Times.Never);
        furniHelper.Verify(
            x => x.BroadcastItemUpdateToRoomAsync(It.IsAny<IRoomLogic>(), It.IsAny<PlayerFurnitureItemPlacementDataDto>()),
            Times.Never);
    }

    [Test]
    public async Task MoveToDirection_FreeTile_MovesItemForward()
    {
        var (strategy, tileHelper, furniHelper) = CreateMoveToDirectionStrategy();

        var item = MockFurnitureItemPlacementData("lamp", 1, 1, id: 4);
        item.Direction = HDirection.East;
        var (room, _) = MockRoom([item]);

        tileHelper
            .Setup(x => x.GetPointInFront(1, 1, HDirection.East, 0))
            .Returns(new Point(2, 1));
        tileHelper
            .Setup(x => x.CanPlaceAt(It.IsAny<IEnumerable<Point>>(), It.IsAny<IRoomTileMap>(),
                It.IsAny<ICollection<PlayerFurnitureItemPlacementDataDto>>(), It.IsAny<bool>()))
            .Returns(true);

        await strategy.ExecuteAsync(room.Object, MockWiredItem("x", selectedItems: [item]), null);

        Assert.Multiple(() =>
        {
            Assert.That(item.PositionX, Is.EqualTo(2));
            Assert.That(item.PositionY, Is.EqualTo(1));
            Assert.That(item.Direction, Is.EqualTo(HDirection.East));
        });

        tileHelper.Verify(x => x.InvalidateItemIndex(It.IsAny<IEnumerable<PlayerFurnitureItemPlacementDataDto>>()), Times.Once);
        furniHelper.Verify(x => x.BroadcastItemUpdateToRoomAsync(room.Object, item), Times.Once);
    }

    [Test]
    public async Task MoveToDirection_Blocked_ReversesDirection()
    {
        var (strategy, tileHelper, furniHelper) = CreateMoveToDirectionStrategy();

        var item = MockFurnitureItemPlacementData("lamp", 1, 1, id: 4);
        item.Direction = HDirection.East;
        var (room, _) = MockRoom([item]);

        tileHelper
            .Setup(x => x.GetPointInFront(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<HDirection>(), 0))
            .Returns(new Point(0, 1));
        tileHelper
            .SetupSequence(x => x.CanPlaceAt(It.IsAny<IEnumerable<Point>>(), It.IsAny<IRoomTileMap>(),
                It.IsAny<ICollection<PlayerFurnitureItemPlacementDataDto>>(), It.IsAny<bool>()))
            .Returns(false)
            .Returns(true);

        await strategy.ExecuteAsync(room.Object, MockWiredItem("x", selectedItems: [item]), null);

        Assert.Multiple(() =>
        {
            Assert.That(item.PositionX, Is.EqualTo(0));
            Assert.That(item.Direction, Is.EqualTo(HDirection.West));
        });

        furniHelper.Verify(x => x.BroadcastItemUpdateToRoomAsync(room.Object, item), Times.Exactly(2));
    }

    [Test]
    public async Task MoveToDirection_FullyBlocked_StaysPut()
    {
        var (strategy, tileHelper, furniHelper) = CreateMoveToDirectionStrategy();

        var item = MockFurnitureItemPlacementData("lamp", 1, 1, id: 4);
        var (room, _) = MockRoom([item]);

        tileHelper
            .Setup(x => x.GetPointInFront(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<HDirection>(), 0))
            .Returns(new Point(0, 1));
        tileHelper
            .Setup(x => x.CanPlaceAt(It.IsAny<IEnumerable<Point>>(), It.IsAny<IRoomTileMap>(),
                It.IsAny<ICollection<PlayerFurnitureItemPlacementDataDto>>(), It.IsAny<bool>()))
            .Returns(false);

        await strategy.ExecuteAsync(room.Object, MockWiredItem("x", selectedItems: [item]), null);

        Assert.Multiple(() =>
        {
            Assert.That(item.PositionX, Is.EqualTo(1));
            Assert.That(item.PositionY, Is.EqualTo(1));
        });

        furniHelper.Verify(
            x => x.BroadcastItemUpdateToRoomAsync(It.IsAny<IRoomLogic>(), It.IsAny<PlayerFurnitureItemPlacementDataDto>()),
            Times.Never);
    }

    [Test]
    public async Task Flee_NoUsersInRoom_DoesNothing()
    {
        var tileHelper = new Mock<IRoomTileMapHelperService>();
        var furniHelper = new Mock<IRoomFurnitureItemHelperService>();
        var strategy = new WiredEffectFleeStrategy(tileHelper.Object, furniHelper.Object);

        var item = MockFurnitureItemPlacementData("lamp", 1, 1, id: 4);
        var (room, _) = MockRoom([item]);

        await strategy.ExecuteAsync(room.Object, MockWiredItem("x", selectedItems: [item]), null);

        tileHelper.Verify(
            x => x.CanPlaceAt(It.IsAny<IEnumerable<Point>>(), It.IsAny<IRoomTileMap>(),
                It.IsAny<ICollection<PlayerFurnitureItemPlacementDataDto>>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Test]
    public async Task Flee_MovesAwayFromClosestUser()
    {
        var tileHelper = new Mock<IRoomTileMapHelperService>();
        var furniHelper = new Mock<IRoomFurnitureItemHelperService>();
        var strategy = new WiredEffectFleeStrategy(tileHelper.Object, furniHelper.Object);

        var item = MockFurnitureItemPlacementData("lamp", 0, 0, id: 4);
        var closeUser = MockUser(id: 1, point: new Point(3, 1));
        var farUser = MockUser(id: 2, point: new Point(10, 10));
        var (room, _) = MockRoom([item], [closeUser.Object, farUser.Object]);

        tileHelper
            .Setup(x => x.GetPointInFront(0, 0, HDirection.West, 0))
            .Returns(new Point(-1, 0));
        tileHelper
            .Setup(x => x.CanPlaceAt(It.IsAny<IEnumerable<Point>>(), It.IsAny<IRoomTileMap>(),
                It.IsAny<ICollection<PlayerFurnitureItemPlacementDataDto>>(), It.IsAny<bool>()))
            .Returns(true);

        await strategy.ExecuteAsync(room.Object, MockWiredItem("x", selectedItems: [item]), null);

        Assert.Multiple(() =>
        {
            Assert.That(item.PositionX, Is.EqualTo(-1));
            Assert.That(item.PositionY, Is.EqualTo(0));
        });

        tileHelper.Verify(x => x.GetPointInFront(0, 0, HDirection.West, 0), Times.Once);
        furniHelper.Verify(x => x.BroadcastItemUpdateToRoomAsync(room.Object, item), Times.Once);
    }

    [Test]
    public async Task MoveRotate_MovementOnly_MovesItem()
    {
        var tileHelper = new Mock<IRoomTileMapHelperService>();
        var furniHelper = new Mock<IRoomFurnitureItemHelperService>();
        var strategy = new WiredEffectMoveRotateStrategy(tileHelper.Object, furniHelper.Object);

        var item = MockFurnitureItemPlacementData("lamp", 1, 1, id: 4);
        var (room, _) = MockRoom([item]);

        tileHelper
            .Setup(x => x.GetPointInFront(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<HDirection>(), 0))
            .Returns(new Point(4, 5));
        tileHelper
            .Setup(x => x.CanPlaceAt(It.IsAny<IEnumerable<Point>>(), It.IsAny<IRoomTileMap>(),
                It.IsAny<ICollection<PlayerFurnitureItemPlacementDataDto>>(), It.IsAny<bool>()))
            .Returns(true);

        await strategy.ExecuteAsync(room.Object, MockWiredItem("x", "1", [item]), null);

        Assert.Multiple(() =>
        {
            Assert.That(item.PositionX, Is.EqualTo(4));
            Assert.That(item.PositionY, Is.EqualTo(5));
            Assert.That(item.Direction, Is.EqualTo(HDirection.North));
        });

        furniHelper.Verify(x => x.BroadcastItemUpdateToRoomAsync(room.Object, item), Times.Once);
    }

    [TestCase("0,1", 2)]
    [TestCase("0,2", 6)]
    public async Task MoveRotate_RotationOnly_RotatesItem(string parameters, int expectedDirection)
    {
        var tileHelper = new Mock<IRoomTileMapHelperService>();
        var furniHelper = new Mock<IRoomFurnitureItemHelperService>();
        var strategy = new WiredEffectMoveRotateStrategy(tileHelper.Object, furniHelper.Object);

        var item = MockFurnitureItemPlacementData("lamp", 1, 1, id: 4);
        var (room, _) = MockRoom([item]);

        await strategy.ExecuteAsync(room.Object, MockWiredItem("x", parameters, [item]), null);

        Assert.That((int)item.Direction, Is.EqualTo(expectedDirection));
        furniHelper.Verify(x => x.BroadcastItemUpdateToRoomAsync(room.Object, item), Times.Once);
    }

    [Test]
    public async Task MoveRotate_RandomRotation_RotatesEitherWay()
    {
        var tileHelper = new Mock<IRoomTileMapHelperService>();
        var furniHelper = new Mock<IRoomFurnitureItemHelperService>();
        var strategy = new WiredEffectMoveRotateStrategy(tileHelper.Object, furniHelper.Object);

        var item = MockFurnitureItemPlacementData("lamp", 1, 1, id: 4);
        var (room, _) = MockRoom([item]);

        await strategy.ExecuteAsync(room.Object, MockWiredItem("x", "0,3", [item]), null);

        Assert.That((int)item.Direction, Is.EqualTo(2).Or.EqualTo(6));
    }

    [Test]
    public async Task MoveRotate_NoParametersOrBlockedMove_StillBroadcasts()
    {
        var tileHelper = new Mock<IRoomTileMapHelperService>();
        var furniHelper = new Mock<IRoomFurnitureItemHelperService>();
        var strategy = new WiredEffectMoveRotateStrategy(tileHelper.Object, furniHelper.Object);

        var item = MockFurnitureItemPlacementData("lamp", 1, 1, id: 4);
        var (room, _) = MockRoom([item]);

        tileHelper
            .Setup(x => x.GetPointInFront(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<HDirection>(), 0))
            .Returns(new Point(4, 5));
        tileHelper
            .Setup(x => x.CanPlaceAt(It.IsAny<IEnumerable<Point>>(), It.IsAny<IRoomTileMap>(),
                It.IsAny<ICollection<PlayerFurnitureItemPlacementDataDto>>(), It.IsAny<bool>()))
            .Returns(false);

        await strategy.ExecuteAsync(room.Object, MockWiredItem("x", "", [item]), null);
        await strategy.ExecuteAsync(room.Object, MockWiredItem("x", "1", [item]), null);

        Assert.That(item.PositionX, Is.EqualTo(1));
        furniHelper.Verify(x => x.BroadcastItemUpdateToRoomAsync(room.Object, item), Times.Exactly(2));
    }

    [Test]
    public void FurnitureHasFurniture_NoSelection_IsNotSatisfied()
    {
        var strategy = new WiredConditionFurnitureHasFurnitureStrategy();
        var negated = new WiredConditionNotFurnitureHasFurnitureStrategy();
        var condition = MockWiredItem(FurnitureItemInteractionType.WiredConditionFurnitureHasFurniture);
        var (room, _) = MockRoom([condition]);

        Assert.Multiple(() =>
        {
            Assert.That(strategy.IsSatisfied(room.Object, condition, null), Is.False);
            Assert.That(negated.IsSatisfied(room.Object, condition, null), Is.True);
        });
    }

    [Test]
    public void FurnitureHasFurniture_ItemStackedAbove_IsSatisfied()
    {
        var strategy = new WiredConditionFurnitureHasFurnitureStrategy();
        var negated = new WiredConditionNotFurnitureHasFurnitureStrategy();

        var baseItem = MockFurnitureItemPlacementData("table", 2, 2, id: 1);
        var stacked = MockFurnitureItemPlacementData("lamp", 2, 2, 1, id: 2);
        var condition = MockWiredItem(
            FurnitureItemInteractionType.WiredConditionFurnitureHasFurniture,
            selectedItems: [baseItem]);
        var (room, _) = MockRoom([condition, baseItem, stacked]);

        Assert.Multiple(() =>
        {
            Assert.That(strategy.IsSatisfied(room.Object, condition, null), Is.True);
            Assert.That(negated.IsSatisfied(room.Object, condition, null), Is.False);
        });
    }

    [Test]
    public void FurnitureHasFurniture_NothingAboveOrItemMissing_IsNotSatisfied()
    {
        var strategy = new WiredConditionFurnitureHasFurnitureStrategy();

        var lonely = MockFurnitureItemPlacementData("table", 2, 2, id: 1);
        var missing = MockFurnitureItemPlacementData("table", 5, 5, id: 9);

        var conditionLonely = MockWiredItem(
            FurnitureItemInteractionType.WiredConditionFurnitureHasFurniture,
            selectedItems: [lonely]);
        var conditionMissing = MockWiredItem(
            FurnitureItemInteractionType.WiredConditionFurnitureHasFurniture,
            selectedItems: [missing]);
        var (room, _) = MockRoom([conditionLonely, conditionMissing, lonely]);

        Assert.Multiple(() =>
        {
            Assert.That(strategy.IsSatisfied(room.Object, conditionLonely, null), Is.False);
            Assert.That(strategy.IsSatisfied(room.Object, conditionMissing, null), Is.False);
        });
    }

    [Test]
    public void TriggererWearsBadge_NoUserOrNoBadgeCode_IsNotSatisfied()
    {
        var strategy = new WiredConditionTriggererWearsBadgeStrategy();
        var negated = new WiredConditionNotTriggererWearsBadgeStrategy();
        var user = MockUser();
        var (room, _) = MockRoom([]);

        var withCode = MockWiredItem("x", message: "ADM");
        var withoutCode = MockWiredItem("x");

        Assert.Multiple(() =>
        {
            Assert.That(strategy.IsSatisfied(room.Object, withCode, null), Is.False);
            Assert.That(strategy.IsSatisfied(room.Object, withoutCode, user.Object), Is.False);
            Assert.That(negated.IsSatisfied(room.Object, withCode, null), Is.True);
        });
    }

    [Test]
    public void TriggererWearsBadge_MatchesCaseInsensitively()
    {
        var strategy = new WiredConditionTriggererWearsBadgeStrategy();
        var negated = new WiredConditionNotTriggererWearsBadgeStrategy();

        var wearer = MockUser(badges: [new PlayerBadgeDto { Badge = new BadgeDto { Code = "ACH_1" } }]);
        var other = MockUser(badges: [new PlayerBadgeDto { Badge = new BadgeDto { Code = "XYZ" } }]);
        var (room, _) = MockRoom([]);

        var condition = MockWiredItem("x", message: "ach_1");

        Assert.Multiple(() =>
        {
            Assert.That(strategy.IsSatisfied(room.Object, condition, wearer.Object), Is.True);
            Assert.That(strategy.IsSatisfied(room.Object, condition, other.Object), Is.False);
            Assert.That(negated.IsSatisfied(room.Object, condition, wearer.Object), Is.False);
        });
    }
}
