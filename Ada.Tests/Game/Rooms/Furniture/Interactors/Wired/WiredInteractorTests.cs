using Ada.API;
using Ada.API.DTOs.Furniture;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Networking;
using Ada.Core.Enums.Game.Furniture;
using Ada.Db.Models.Constants;
using Ada.Game.Rooms.Furniture.Interactors.Wired;
using Ada.Networking.Writers.Rooms.Furniture;
using Moq;

namespace Ada.Tests.Game.Rooms.Furniture.Interactors.Wired;

public abstract class WiredInteractorTestBase
{
    protected static readonly ServerRoomConstants RoomConstants = new() { WiredMaxFurnitureSelection = 5 };

    protected static PlayerFurnitureItemPlacementDataDto MakeItem(
        string? interactionType,
        int assetId = 0,
        int id = 0,
        bool withWiredData = false,
        string message = "",
        string intParameters = "",
        int delay = 0,
        List<PlayerFurnitureItemPlacementDataDto>? selectedItems = null)
    {
        var item = new PlayerFurnitureItemPlacementDataDto
        {
            Id = id,
            PlayerFurnitureItem = new PlayerFurnitureItemDto
            {
                FurnitureItemId = 0,
                FurnitureItem = new FurnitureItemDto
                {
                    InteractionType = interactionType,
                    Name = "",
                    AssetName = "",
                    AssetId = assetId
                },
                LimitedData = "",
                MetaData = ""
            }
        };

        if (withWiredData)
        {
            item.WiredData = new PlayerFurnitureItemWiredDataDto
            {
                PlayerFurnitureItemPlacementDataId = item.Id,
                PlacementData = item,
                Message = message,
                IntParameters = intParameters,
                Delay = delay,
                SelectedItems = selectedItems ?? []
            };
        }

        return item;
    }

    protected static (IRoomUser User, List<AbstractPacketWriter> Written) MakeUser()
    {
        var written = new List<AbstractPacketWriter>();

        var network = new Mock<INetworkObject>();
        network
            .Setup(x => x.WriteToStreamAsync(It.IsAny<AbstractPacketWriter>()))
            .Callback<AbstractPacketWriter>(written.Add)
            .Returns(Task.CompletedTask);

        var user = new Mock<IRoomUser>();
        user.SetupGet(x => x.NetworkObject).Returns(network.Object);

        return (user.Object, written);
    }

    protected static Mock<IRoomWiredService> MakeWiredService(int code = 42)
    {
        var wiredService = new Mock<IRoomWiredService>();
        wiredService.Setup(x => x.GetWiredCode(It.IsAny<string>())).Returns(code);
        return wiredService;
    }
}

[TestFixture]
public class GenericWiredConditionInteractorTests : WiredInteractorTestBase
{
    [Test]
    public void InteractionTypes_ContainsAllConditionTypes()
    {
        var interactor = new GenericWiredConditionInteractor(MakeWiredService().Object, RoomConstants);

        Assert.That(interactor.InteractionTypes, Has.Count.EqualTo(16));
        Assert.That(interactor.InteractionTypes, Does.Contain(FurnitureItemInteractionType.WiredConditionFurnitureHasUsers));
        Assert.That(interactor.InteractionTypes, Does.Contain(FurnitureItemInteractionType.WiredConditionDateRangeActive));
    }

    [Test]
    public async Task OnTriggerAsync_WithWiredData_WritesConfig()
    {
        var wiredService = MakeWiredService(7);
        var interactor = new GenericWiredConditionInteractor(wiredService.Object, RoomConstants);
        var (user, written) = MakeUser();
        var selected = new List<PlayerFurnitureItemPlacementDataDto>
        {
            MakeItem(FurnitureItemInteractionType.Gate, id: 11),
            MakeItem(FurnitureItemInteractionType.Gate, id: 12)
        };
        var item = MakeItem(FurnitureItemInteractionType.WiredConditionFurnitureHasUsers,
            assetId: 3, id: 21, withWiredData: true, message: "cfg", intParameters: "5,8", selectedItems: selected);

        await interactor.OnTriggerAsync(Mock.Of<IRoomLogic>(), item, user);

        var writer = (WiredConditionWriter)written.Single();
        Assert.Multiple(() =>
        {
            Assert.That(writer.StuffTypeSelectionEnabled, Is.False);
            Assert.That(writer.MaxItemsSelected, Is.EqualTo(5));
            Assert.That(writer.SelectedItemIds, Is.EqualTo(new[] { 11, 12 }));
            Assert.That(writer.AssetId, Is.EqualTo(3));
            Assert.That(writer.Id, Is.EqualTo(21));
            Assert.That(writer.Input, Is.EqualTo("cfg"));
            Assert.That(writer.IntParameters, Is.EqualTo(new[] { 5, 8 }));
            Assert.That(writer.ConditionConfig, Is.EqualTo(7));
        });
        wiredService.Verify(x => x.GetWiredCode(FurnitureItemInteractionType.WiredConditionFurnitureHasUsers), Times.Once);
    }

    [Test]
    public async Task OnTriggerAsync_NullWiredDataAndInteractionType_WritesDefaults()
    {
        var wiredService = MakeWiredService();
        var interactor = new GenericWiredConditionInteractor(wiredService.Object, RoomConstants);
        var (user, written) = MakeUser();
        var item = MakeItem(null);

        await interactor.OnTriggerAsync(Mock.Of<IRoomLogic>(), item, user);

        var writer = (WiredConditionWriter)written.Single();
        Assert.Multiple(() =>
        {
            Assert.That(writer.SelectedItemIds, Is.Empty);
            Assert.That(writer.Input, Is.EqualTo(""));
            Assert.That(writer.IntParameters, Is.Empty);
        });
        wiredService.Verify(x => x.GetWiredCode(""), Times.Once);
    }
}

[TestFixture]
public class GenericWiredEffectInteractorTests : WiredInteractorTestBase
{
    [Test]
    public void InteractionTypes_ContainsAllEffectTypes()
    {
        var interactor = new GenericWiredEffectInteractor(MakeWiredService().Object, RoomConstants);

        Assert.That(interactor.InteractionTypes, Has.Count.EqualTo(9));
        Assert.That(interactor.InteractionTypes, Does.Contain(FurnitureItemInteractionType.WiredEffectToggleFurnitureState));
        Assert.That(interactor.InteractionTypes, Does.Contain(FurnitureItemInteractionType.WiredEffectChangeFurnitureDirection));
    }

    [Test]
    public async Task OnTriggerAsync_WithWiredData_WritesConfig()
    {
        var wiredService = MakeWiredService(9);
        var interactor = new GenericWiredEffectInteractor(wiredService.Object, RoomConstants);
        var (user, written) = MakeUser();
        var selected = new List<PlayerFurnitureItemPlacementDataDto> { MakeItem(FurnitureItemInteractionType.Gate, id: 31) };
        var item = MakeItem(FurnitureItemInteractionType.WiredEffectToggleFurnitureState,
            assetId: 4, id: 22, withWiredData: true, message: "fx", intParameters: "1,0,3", delay: 3, selectedItems: selected);

        await interactor.OnTriggerAsync(Mock.Of<IRoomLogic>(), item, user);

        var writer = (WiredMessageEffectWriter)written.Single();
        Assert.Multiple(() =>
        {
            Assert.That(writer.MaxItemsSelected, Is.EqualTo(5));
            Assert.That(writer.SelectedItemIds, Is.EqualTo(new[] { 31 }));
            Assert.That(writer.WiredEffectType, Is.EqualTo(4));
            Assert.That(writer.Id, Is.EqualTo(22));
            Assert.That(writer.Input, Is.EqualTo("fx"));
            Assert.That(writer.IntParams, Is.EqualTo(new[] { 1, 0, 3 }));
            Assert.That(writer.Type, Is.EqualTo(9));
            Assert.That(writer.DelayInPulses, Is.EqualTo(3));
            Assert.That(writer.ConflictingTriggerIds, Is.Empty);
        });
        wiredService.Verify(x => x.GetWiredCode(FurnitureItemInteractionType.WiredEffectToggleFurnitureState), Times.Once);
    }

    [Test]
    public async Task OnTriggerAsync_NullWiredDataAndInteractionType_WritesDefaults()
    {
        var wiredService = MakeWiredService();
        var interactor = new GenericWiredEffectInteractor(wiredService.Object, RoomConstants);
        var (user, written) = MakeUser();
        var item = MakeItem(null);

        await interactor.OnTriggerAsync(Mock.Of<IRoomLogic>(), item, user);

        var writer = (WiredMessageEffectWriter)written.Single();
        Assert.Multiple(() =>
        {
            Assert.That(writer.SelectedItemIds, Is.Empty);
            Assert.That(writer.Input, Is.EqualTo(""));
            Assert.That(writer.IntParams, Is.Empty);
            Assert.That(writer.DelayInPulses, Is.EqualTo(0));
        });
        wiredService.Verify(x => x.GetWiredCode(""), Times.Once);
    }
}

[TestFixture]
public class GenericWiredTriggerInteractorTests : WiredInteractorTestBase
{
    [Test]
    public void InteractionTypes_ContainsAllTriggerTypes()
    {
        var interactor = new GenericWiredTriggerInteractor(MakeWiredService().Object, RoomConstants);

        Assert.That(interactor.InteractionTypes, Has.Count.EqualTo(7));
        Assert.That(interactor.InteractionTypes, Does.Contain(FurnitureItemInteractionType.WiredTriggerSaysSomething));
        Assert.That(interactor.InteractionTypes, Does.Contain(FurnitureItemInteractionType.WiredTriggerFurnitureStateChanged));
    }

    [Test]
    public async Task OnTriggerAsync_WithWiredData_WritesConfigWithZeroIds()
    {
        var wiredService = MakeWiredService(3);
        var interactor = new GenericWiredTriggerInteractor(wiredService.Object, RoomConstants);
        var (user, written) = MakeUser();
        var selected = new List<PlayerFurnitureItemPlacementDataDto> { MakeItem(FurnitureItemInteractionType.Gate, id: 41) };
        var item = MakeItem(FurnitureItemInteractionType.WiredTriggerSaysSomething,
            assetId: 6, id: 23, withWiredData: true, message: "say", intParameters: "2", selectedItems: selected);

        await interactor.OnTriggerAsync(Mock.Of<IRoomLogic>(), item, user);

        var writer = (WiredTriggerWriter)written.Single();
        Assert.Multiple(() =>
        {
            Assert.That(writer.MaxItemsSelected, Is.EqualTo(5));
            Assert.That(writer.SelectedItemIds, Is.EqualTo(new[] { 41 }));
            Assert.That(writer.AssetId, Is.EqualTo(0));
            Assert.That(writer.Id, Is.EqualTo(0));
            Assert.That(writer.Input, Is.EqualTo("say"));
            Assert.That(writer.IntParameters, Is.EqualTo(new[] { 2 }));
            Assert.That(writer.TriggerConfig, Is.EqualTo(3));
            Assert.That(writer.ConflictingEffectIds, Is.Empty);
        });
        wiredService.Verify(x => x.GetWiredCode(FurnitureItemInteractionType.WiredTriggerSaysSomething), Times.Once);
    }

    [Test]
    public async Task OnTriggerAsync_NullWiredDataAndInteractionType_WritesDefaults()
    {
        var wiredService = MakeWiredService();
        var interactor = new GenericWiredTriggerInteractor(wiredService.Object, RoomConstants);
        var (user, written) = MakeUser();
        var item = MakeItem(null);

        await interactor.OnTriggerAsync(Mock.Of<IRoomLogic>(), item, user);

        var writer = (WiredTriggerWriter)written.Single();
        Assert.Multiple(() =>
        {
            Assert.That(writer.SelectedItemIds, Is.Empty);
            Assert.That(writer.Input, Is.EqualTo(""));
            Assert.That(writer.IntParameters, Is.Empty);
        });
        wiredService.Verify(x => x.GetWiredCode(""), Times.Once);
    }
}

[TestFixture]
public class WiredTriggerUserWalksOnInteractorTests : WiredInteractorTestBase
{
    [Test]
    public void InteractionTypes_IsWalksOnFurniture()
    {
        var interactor = new WiredTriggerUserWalksOnInteractor(MakeWiredService().Object, RoomConstants);

        Assert.That(interactor.InteractionTypes,
            Is.EqualTo(new[] { FurnitureItemInteractionType.WiredTriggerUserWalksOnFurniture }));
    }

    [Test]
    public async Task OnTriggerAsync_WithWiredData_WritesConfigWithItemIds()
    {
        var wiredService = MakeWiredService(4);
        var interactor = new WiredTriggerUserWalksOnInteractor(wiredService.Object, RoomConstants);
        var (user, written) = MakeUser();
        var selected = new List<PlayerFurnitureItemPlacementDataDto> { MakeItem(FurnitureItemInteractionType.Gate, id: 51) };
        var item = MakeItem(FurnitureItemInteractionType.WiredTriggerUserWalksOnFurniture,
            assetId: 8, id: 24, withWiredData: true, selectedItems: selected);

        await interactor.OnTriggerAsync(Mock.Of<IRoomLogic>(), item, user);

        var writer = (WiredTriggerWriter)written.Single();
        Assert.Multiple(() =>
        {
            Assert.That(writer.MaxItemsSelected, Is.EqualTo(5));
            Assert.That(writer.SelectedItemIds, Is.EqualTo(new[] { 51 }));
            Assert.That(writer.AssetId, Is.EqualTo(8));
            Assert.That(writer.Id, Is.EqualTo(24));
            Assert.That(writer.Input, Is.EqualTo(""));
            Assert.That(writer.IntParameters, Is.Empty);
            Assert.That(writer.TriggerConfig, Is.EqualTo(4));
        });
        wiredService.Verify(x => x.GetWiredCode(FurnitureItemInteractionType.WiredTriggerUserWalksOnFurniture), Times.Once);
    }

    [Test]
    public async Task OnTriggerAsync_NullWiredDataAndInteractionType_WritesDefaults()
    {
        var wiredService = MakeWiredService();
        var interactor = new WiredTriggerUserWalksOnInteractor(wiredService.Object, RoomConstants);
        var (user, written) = MakeUser();
        var item = MakeItem(null, assetId: 8, id: 24);

        await interactor.OnTriggerAsync(Mock.Of<IRoomLogic>(), item, user);

        var writer = (WiredTriggerWriter)written.Single();
        Assert.Multiple(() =>
        {
            Assert.That(writer.SelectedItemIds, Is.Empty);
            Assert.That(writer.AssetId, Is.EqualTo(8));
        });
        wiredService.Verify(x => x.GetWiredCode(""), Times.Once);
    }
}

[TestFixture]
public class WiredEffectShowMessageInteractorTests : WiredInteractorTestBase
{
    [Test]
    public void InteractionTypes_IsShowMessage()
    {
        var interactor = new WiredEffectShowMessageInteractor(MakeWiredService().Object);

        Assert.That(interactor.InteractionTypes,
            Is.EqualTo(new[] { FurnitureItemInteractionType.WiredEffectShowMessage }));
    }

    [Test]
    public async Task OnTriggerAsync_WithWiredData_WritesConfig()
    {
        var wiredService = MakeWiredService(6);
        var interactor = new WiredEffectShowMessageInteractor(wiredService.Object);
        var (user, written) = MakeUser();
        var item = MakeItem(FurnitureItemInteractionType.WiredEffectShowMessage,
            assetId: 12, id: 25, withWiredData: true, message: "hello");

        await interactor.OnTriggerAsync(Mock.Of<IRoomLogic>(), item, user);

        var writer = (WiredMessageEffectWriter)written.Single();
        Assert.Multiple(() =>
        {
            Assert.That(writer.MaxItemsSelected, Is.EqualTo(0));
            Assert.That(writer.SelectedItemIds, Is.Empty);
            Assert.That(writer.WiredEffectType, Is.EqualTo(12));
            Assert.That(writer.Id, Is.EqualTo(25));
            Assert.That(writer.Input, Is.EqualTo("hello"));
            Assert.That(writer.IntParams, Is.Empty);
            Assert.That(writer.Type, Is.EqualTo(6));
            Assert.That(writer.DelayInPulses, Is.EqualTo(0));
        });
        wiredService.Verify(x => x.GetWiredCode(FurnitureItemInteractionType.WiredEffectShowMessage), Times.Once);
    }

    [Test]
    public async Task OnTriggerAsync_NullWiredDataAndInteractionType_WritesDefaults()
    {
        var wiredService = MakeWiredService();
        var interactor = new WiredEffectShowMessageInteractor(wiredService.Object);
        var (user, written) = MakeUser();
        var item = MakeItem(null);

        await interactor.OnTriggerAsync(Mock.Of<IRoomLogic>(), item, user);

        var writer = (WiredMessageEffectWriter)written.Single();
        Assert.That(writer.Input, Is.EqualTo(""));
        wiredService.Verify(x => x.GetWiredCode(""), Times.Once);
    }
}

[TestFixture]
public class WiredEffectKickUserInteractorTests : WiredInteractorTestBase
{
    [Test]
    public void InteractionTypes_IsKickUser()
    {
        var interactor = new WiredEffectKickUserInteractor(MakeWiredService().Object);

        Assert.That(interactor.InteractionTypes,
            Is.EqualTo(new[] { FurnitureItemInteractionType.WiredEffectKickUser }));
    }

    [Test]
    public async Task OnTriggerAsync_WithWiredData_WritesConfig()
    {
        var wiredService = MakeWiredService(8);
        var interactor = new WiredEffectKickUserInteractor(wiredService.Object);
        var (user, written) = MakeUser();
        var item = MakeItem(FurnitureItemInteractionType.WiredEffectKickUser,
            assetId: 14, id: 26, withWiredData: true, message: "bye");

        await interactor.OnTriggerAsync(Mock.Of<IRoomLogic>(), item, user);

        var writer = (WiredMessageEffectWriter)written.Single();
        Assert.Multiple(() =>
        {
            Assert.That(writer.MaxItemsSelected, Is.EqualTo(5));
            Assert.That(writer.SelectedItemIds, Is.Empty);
            Assert.That(writer.WiredEffectType, Is.EqualTo(14));
            Assert.That(writer.Id, Is.EqualTo(26));
            Assert.That(writer.Input, Is.EqualTo("bye"));
            Assert.That(writer.Type, Is.EqualTo(8));
        });
        wiredService.Verify(x => x.GetWiredCode(FurnitureItemInteractionType.WiredEffectKickUser), Times.Once);
    }

    [Test]
    public async Task OnTriggerAsync_NullWiredDataAndInteractionType_WritesDefaults()
    {
        var wiredService = MakeWiredService();
        var interactor = new WiredEffectKickUserInteractor(wiredService.Object);
        var (user, written) = MakeUser();
        var item = MakeItem(null);

        await interactor.OnTriggerAsync(Mock.Of<IRoomLogic>(), item, user);

        var writer = (WiredMessageEffectWriter)written.Single();
        Assert.That(writer.Input, Is.EqualTo(""));
        wiredService.Verify(x => x.GetWiredCode(""), Times.Once);
    }
}
