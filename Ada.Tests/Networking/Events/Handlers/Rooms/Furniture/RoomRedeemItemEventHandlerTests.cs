using Ada.API.DTOs.Furniture;
using Ada.API.DTOs.Players;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Client;
using Ada.Networking.Events.Handlers.Rooms.Furniture;
using Ada.Tests.Common;
using Moq;

namespace Ada.Tests.Networking.Events.Handlers.Rooms.Furniture;

[TestFixture]
public class RoomRedeemItemEventHandlerTests
{
    private const long _ownerId = 1;
    private const long _visitorId = 2;

    private SqliteTestDbFactory _dbFactory = null!;

    [SetUp]
    public void SetUp() => _dbFactory = new SqliteTestDbFactory();

    [TearDown]
    public void TearDown() => _dbFactory.Dispose();

    [Test]
    public async Task Redeem_ItemOwnedByAnotherPlayer_IsRefused()
    {
        var item = ExchangeableItem(itemOwnerId: _visitorId, assetName: "CF_50_gold");
        var harness = Create(item, callerId: _ownerId);

        await harness.Handler.HandleAsync(harness.Client);

        Assert.Multiple(() =>
        {
            Assert.That(harness.CallerData.CreditBalance, Is.Zero,
                "redeeming another player's item must not credit the room owner");
            Assert.That(harness.RoomItems, Does.Contain(item),
                "the victim's item must not be removed from the room");
        });
    }

    [Test]
    public async Task Redeem_OwnCreditItem_CreditsPlayerAndClearsInventory()
    {
        var item = ExchangeableItem(itemOwnerId: _ownerId, assetName: "CF_50_gold");
        var harness = Create(item, callerId: _ownerId);
        harness.Inventory.Add(item.PlayerFurnitureItem);

        await harness.Handler.HandleAsync(harness.Client);

        Assert.Multiple(() =>
        {
            Assert.That(harness.CallerData.CreditBalance, Is.EqualTo(50));
            Assert.That(harness.RoomItems, Does.Not.Contain(item));
            Assert.That(harness.Inventory, Is.Empty,
                "a redeemed item left in the inventory can be re-placed after deletion");
        });
    }

    [Test]
    public async Task Redeem_OwnPixelItem_CreditsPixelsRatherThanNothing()
    {
        var item = ExchangeableItem(itemOwnerId: _ownerId, assetName: "PF_25_pixel");
        var harness = Create(item, callerId: _ownerId);

        await harness.Handler.HandleAsync(harness.Client);

        Assert.Multiple(() =>
        {
            Assert.That(harness.CallerData.PixelBalance, Is.EqualTo(25));
            Assert.That(harness.CallerData.CreditBalance, Is.Zero);
        });
    }

    [Test]
    public async Task Redeem_MalformedAssetName_IsRefusedWithoutThrowing()
    {
        var item = ExchangeableItem(itemOwnerId: _ownerId, assetName: "DF_5");
        var harness = Create(item, callerId: _ownerId);

        Assert.DoesNotThrowAsync(() => harness.Handler.HandleAsync(harness.Client));

        Assert.That(harness.RoomItems, Does.Contain(item));
        await Task.CompletedTask;
    }

    [Test]
    public async Task Redeem_NonExchangeableItem_IsRefused()
    {
        var item = ExchangeableItem(itemOwnerId: _ownerId, assetName: "chair_basic");
        var harness = Create(item, callerId: _ownerId);

        await harness.Handler.HandleAsync(harness.Client);

        Assert.Multiple(() =>
        {
            Assert.That(harness.CallerData.CreditBalance, Is.Zero);
            Assert.That(harness.RoomItems, Does.Contain(item));
        });
    }

    private sealed record Harness(
        RoomRedeemItemEventHandler Handler,
        INetworkClient Client,
        PlayerDataDto CallerData,
        ICollection<PlayerFurnitureItemPlacementDataDto> RoomItems,
        ICollection<PlayerFurnitureItemDto> Inventory);

    private Harness Create(PlayerFurnitureItemPlacementDataDto item, long callerId)
    {
        var callerData = new PlayerDataDto();
        var caller = PlayerDto(callerId, callerData);
        var roomDto = new RoomDto { Id = 10, OwnerId = _ownerId, FurnitureItems = [item] };
        var roomItems = roomDto.FurnitureItems;

        var room = new Mock<IRoomLogic>();
        room.SetupGet(x => x.Room).Returns(roomDto);
        room.Setup(x => x.BroadcastDataAsync(It.IsAny<AbstractPacketWriter>())).Returns(Task.CompletedTask);

        var playerLogic = new Mock<IPlayerLogic>();
        playerLogic.SetupGet(x => x.Player).Returns(caller);
        playerLogic.SetupGet(x => x.NetworkObject).Returns(Mock.Of<INetworkObject>());

        var roomUser = new Mock<IRoomUser>();
        roomUser.SetupGet(x => x.Room).Returns(room.Object);

        var client = new Mock<INetworkClient>();
        client.SetupGet(x => x.Player).Returns(playerLogic.Object);
        client.SetupGet(x => x.RoomUser).Returns(roomUser.Object);
        client.Setup(x => x.WriteToStreamAsync(It.IsAny<AbstractPacketWriter>())).Returns(Task.CompletedTask);

        var handler = new RoomRedeemItemEventHandler(_dbFactory)
        {
            ItemId = item.PlayerFurnitureItemId
        };

        return new Harness(handler, client.Object, callerData, roomItems, caller.FurnitureItems);
    }

    private static PlayerFurnitureItemPlacementDataDto ExchangeableItem(long itemOwnerId, string assetName) => new()
    {
        Id = 500,
        PlayerFurnitureItemId = 77,
        PlayerFurnitureItem = new PlayerFurnitureItemDto
        {
            Id = 77,
            PlayerId = itemOwnerId,
            FurnitureItemId = 0,
            LimitedData = "",
            MetaData = "",
            FurnitureItem = new FurnitureItemDto
            {
                Name = "",
                AssetName = assetName,
                InteractionType = ""
            }
        }
    };

    private static PlayerDto PlayerDto(long id, PlayerDataDto data) => new(
        id, $"player{id}", "", DateTimeOffset.UtcNow, [], data, new PlayerAvatarDataDto(), [], [], [], [],
        new PlayerNavigatorSettingsDto(), new PlayerGameSettingsDto(), [], [], [], [], [], [], [], [], [], [], [],
        [], [], [], [], [], []);
}
