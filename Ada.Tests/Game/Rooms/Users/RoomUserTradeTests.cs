using Ada.API.DTOs.Furniture;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Game.Rooms.Users;
using Ada.Tests.Common;
using Ada.Tests.Game.Catalog;
using Moq;

namespace Ada.Tests.Game.Rooms.Users;

[TestFixture]
public class RoomUserTradeTests
{
    private const long _userOneId = 1;
    private const long _userTwoId = 2;

    private SqliteTestDbFactory _dbFactory = null!;
    private Mock<IPlayerHelperService> _playerHelperService = null!;

    [SetUp]
    public void SetUp()
    {
        _dbFactory = new SqliteTestDbFactory();
        _playerHelperService = new Mock<IPlayerHelperService>();
    }

    [TearDown]
    public void TearDown() => _dbFactory.Dispose();

    [Test]
    public async Task SwapItems_PlacedItem_IsRejected()
    {
        var item = ItemDto(10, _userOneId);
        item.PlacementData = new PlayerFurnitureItemPlacementDataDto { PlayerFurnitureItem = item };

        var trade = MakeTrade([item], oneInventory: [item], twoInventory: []);

        Assert.That(await trade.SwapItemsAsync(), Is.False,
            "an item placed in a room since the offer must not change hands");
    }

    [Test]
    public async Task SwapItems_ItemNoLongerInTheOfferersInventory_IsRejected()
    {
        var item = ItemDto(10, _userOneId);
        var trade = MakeTrade([item], oneInventory: [], twoInventory: []);

        Assert.That(await trade.SwapItemsAsync(), Is.False);
    }

    [Test]
    public async Task SwapItems_ItemBelongingToNeitherTrader_IsRejected()
    {
        var item = ItemDto(10, playerId: 99);
        var trade = MakeTrade([item], oneInventory: [item], twoInventory: []);

        Assert.That(await trade.SwapItemsAsync(), Is.False);
    }

    [Test]
    public async Task SwapItems_RejectedSwap_NotifiesNobody()
    {
        var item = ItemDto(10, _userOneId);
        var trade = MakeTrade([item], oneInventory: [], twoInventory: []);

        await trade.SwapItemsAsync();

        _playerHelperService.Verify(
            x => x.RefreshInventoryAsync(It.IsAny<IPlayerLogic>()), Times.Never);
        _playerHelperService.Verify(
            x => x.SendUnseenInventoryItemsAsync(It.IsAny<IPlayerLogic>(),
                It.IsAny<List<PlayerFurnitureItemDto>>()), Times.Never);
    }

    private static PlayerFurnitureItemDto ItemDto(int id, long playerId) => new()
    {
        Id = id,
        PlayerId = playerId,
        FurnitureItemId = 0,
        LimitedData = "",
        MetaData = "",
        FurnitureItem = new FurnitureItemDto { Name = "chair", AssetName = "chair", InteractionType = "default" }
    };

    private RoomUserTrade MakeTrade(
        List<PlayerFurnitureItemDto> offered,
        List<PlayerFurnitureItemDto> oneInventory,
        List<PlayerFurnitureItemDto> twoInventory) =>
        new(_playerHelperService.Object, _dbFactory, NullLogger<RoomUserTrade>.Instance)
        {
            Users =
            [
                MakeRoomUser(MakePlayerLogic(_userOneId, oneInventory)),
                MakeRoomUser(MakePlayerLogic(_userTwoId, twoInventory))
            ],
            Items = offered
        };

    private static IPlayerLogic MakePlayerLogic(long id, List<PlayerFurnitureItemDto> inventory)
    {
        var logic = new Mock<IPlayerLogic>();
        logic.SetupGet(x => x.Player)
            .Returns(CatalogClientFactory.MakePlayerDto(id, $"user{id}", furnitureItems: inventory));

        return logic.Object;
    }

    private static IRoomUser MakeRoomUser(IPlayerLogic player)
    {
        var user = new Mock<IRoomUser>();
        user.SetupGet(x => x.Player).Returns(player);

        return user.Object;
    }
}
