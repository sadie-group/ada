using Ada.API.DTOs.Catalog.Items;
using Ada.API.DTOs.Catalog.Pages;
using Ada.API.DTOs.Furniture;
using Ada.API.Interfaces.Game.Catalog;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Client;
using Ada.Core.Enums.Game.Catalog;
using Ada.Core.Enums.Game.Furniture;
using Ada.Networking.Events.Handlers.Catalog;
using Ada.Tests.Game.Catalog;
using Moq;

namespace Ada.Tests.Networking.Events.Handlers.Catalog;

[TestFixture]
public class CatalogPurchaseEventHandlerTests
{
    private const int _pageId = 4;
    private const int _itemId = 9;

    private Mock<ICatalogChargeService> _chargeService = null!;
    private Mock<ICatalogFurniturePurchaseService> _furnitureService = null!;
    private Mock<ICatalogBotPurchaseService> _botService = null!;
    private Mock<ICatalogTeleportPurchaseService> _teleportService = null!;
    private Mock<ICatalogPurchaseConfirmationService> _confirmationService = null!;

    [SetUp]
    public void SetUp()
    {
        _chargeService = new Mock<ICatalogChargeService>();
        _chargeService.Setup(x => x.HasRequiredMembership(It.IsAny<INetworkClient>(), It.IsAny<CatalogItemDto>()))
            .Returns(true);
        _chargeService
            .Setup(x => x.TryChargeAsync(It.IsAny<INetworkClient>(), It.IsAny<CatalogItemDto>(), It.IsAny<int>()))
            .ReturnsAsync(true);
        _chargeService
            .Setup(x => x.RefundAsync(It.IsAny<INetworkClient>(), It.IsAny<CatalogItemDto>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        _furnitureService = new Mock<ICatalogFurniturePurchaseService>();
        _botService = new Mock<ICatalogBotPurchaseService>();
        _teleportService = new Mock<ICatalogTeleportPurchaseService>();

        _confirmationService = new Mock<ICatalogPurchaseConfirmationService>();
        _confirmationService.Setup(x => x.WriteFailureAsync(It.IsAny<INetworkClient>())).Returns(Task.CompletedTask);
        _confirmationService.Setup(x => x.WriteUnavailableAsync(It.IsAny<INetworkClient>())).Returns(Task.CompletedTask);
    }

    [Test]
    public async Task Handle_BotPurchase_ChargesForOneBotRegardlessOfRequestedAmount()
    {
        var (client, handler) = Create(BotPage(), amount: 50);

        await handler.HandleAsync(client);

        _chargeService.Verify(x => x.TryChargeAsync(client, It.IsAny<CatalogItemDto>(), 1), Times.Once);
        _chargeService.Verify(
            x => x.TryChargeAsync(client, It.IsAny<CatalogItemDto>(), It.Is<int>(a => a != 1)), Times.Never);
    }

    [Test]
    public async Task Handle_TeleportPurchase_ChargesForOnePairRegardlessOfRequestedAmount()
    {
        var (client, handler) = Create(TeleportPage(), amount: 50);

        await handler.HandleAsync(client);

        _chargeService.Verify(x => x.TryChargeAsync(client, It.IsAny<CatalogItemDto>(), 1), Times.Once);
        _teleportService.Verify(
            x => x.ProcessAsync(client, It.IsAny<CatalogItemDto>(), It.IsAny<string?>(), 1), Times.Once);
    }

    [Test]
    public async Task Handle_FurniturePurchase_ChargesForTheFullAmount()
    {
        var (client, handler) = Create(FurniturePage(), amount: 7);

        await handler.HandleAsync(client);

        _chargeService.Verify(x => x.TryChargeAsync(client, It.IsAny<CatalogItemDto>(), 7), Times.Once);
        _furnitureService.Verify(
            x => x.ProcessAsync(client, It.IsAny<CatalogItemDto>(), It.IsAny<string?>(), 7), Times.Once);
    }

    [Test]
    public async Task Handle_ClubPurchase_IsChargedBeforeItIsDelivered()
    {
        var vipService = new Mock<ICatalogVipPurchaseService>();
        var (client, handler) = Create(VipPage(), amount: 1, vipService: vipService);

        await handler.HandleAsync(client);

        _chargeService.Verify(x => x.TryChargeAsync(client, It.IsAny<CatalogItemDto>(), 1), Times.Once);
        vipService.Verify(x => x.ProcessAsync(client, It.IsAny<CatalogItemDto>()), Times.Once);
    }

    [Test]
    public async Task Handle_ClubPurchaseChargeRejected_DeliversNothing()
    {
        _chargeService
            .Setup(x => x.TryChargeAsync(It.IsAny<INetworkClient>(), It.IsAny<CatalogItemDto>(), It.IsAny<int>()))
            .ReturnsAsync(false);

        var vipService = new Mock<ICatalogVipPurchaseService>();
        var (client, handler) = Create(VipPage(), amount: 1, vipService: vipService);

        await handler.HandleAsync(client);

        vipService.Verify(
            x => x.ProcessAsync(It.IsAny<INetworkClient>(), It.IsAny<CatalogItemDto>()), Times.Never);
        _confirmationService.Verify(x => x.WriteFailureAsync(client), Times.Once);
    }

    private static CatalogPageDto VipPage() => new()
    {
        Id = _pageId,
        Enabled = true,
        Visible = true,
        Layout = CatalogPageLayout.VipBuy,
        Items =
        [
            new CatalogItemDto { Id = _itemId, Name = "club", CostCredits = 25 }
        ]
    };

    [Test]
    public async Task Handle_ChargeRejected_TellsTheClientThePurchaseFailed()
    {
        _chargeService
            .Setup(x => x.TryChargeAsync(It.IsAny<INetworkClient>(), It.IsAny<CatalogItemDto>(), It.IsAny<int>()))
            .ReturnsAsync(false);

        var (client, handler) = Create(FurniturePage(), amount: 1);

        await handler.HandleAsync(client);

        _confirmationService.Verify(x => x.WriteFailureAsync(client), Times.Once);
        _furnitureService.Verify(
            x => x.ProcessAsync(It.IsAny<INetworkClient>(), It.IsAny<CatalogItemDto>(), It.IsAny<string?>(),
                It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task Handle_DeliveryThrows_RefundsExactlyWhatWasCharged()
    {
        _botService.Setup(x => x.ProcessAsync(It.IsAny<INetworkClient>(), It.IsAny<CatalogItemDto>()))
            .ThrowsAsync(new InvalidOperationException("delivery failed"));

        var (client, handler) = Create(BotPage(), amount: 50);

        await handler.HandleAsync(client);

        _chargeService.Verify(x => x.RefundAsync(client, It.IsAny<CatalogItemDto>(), 1), Times.Once);
        _confirmationService.Verify(x => x.WriteFailureAsync(client), Times.Once);
    }

    [Test]
    public async Task Handle_AmountAboveCap_IsRejectedBeforeCharging()
    {
        var (client, handler) = Create(FurniturePage(), amount: 10_000);

        await handler.HandleAsync(client);

        _chargeService.Verify(
            x => x.TryChargeAsync(It.IsAny<INetworkClient>(), It.IsAny<CatalogItemDto>(), It.IsAny<int>()),
            Times.Never);
        _confirmationService.Verify(x => x.WriteFailureAsync(client), Times.Once);
    }

    private static CatalogPageDto BotPage() => new()
    {
        Id = _pageId,
        Enabled = true,
        Visible = true,
        Layout = CatalogPageLayout.Bots,
        Items =
        [
            new CatalogItemDto
            {
                Id = _itemId,
                Name = "bot_barman",
                MetaData = "barman",
                CostCredits = 10
            }
        ]
    };

    private static CatalogPageDto TeleportPage() => new()
    {
        Id = _pageId,
        Enabled = true,
        Visible = true,
        Layout = "default",
        Items =
        [
            new CatalogItemDto
            {
                Id = _itemId,
                Name = "tele",
                CostCredits = 10,
                FurnitureItems =
                [
                    new FurnitureItemDto
                    {
                        Name = "tele",
                        AssetName = "tele",
                        InteractionType = FurnitureItemInteractionType.Teleport
                    }
                ]
            }
        ]
    };

    private static CatalogPageDto FurniturePage() => new()
    {
        Id = _pageId,
        Enabled = true,
        Visible = true,
        Layout = "default",
        Items =
        [
            new CatalogItemDto
            {
                Id = _itemId,
                Name = "chair",
                CostCredits = 10,
                FurnitureItems =
                [
                    new FurnitureItemDto { Name = "chair", AssetName = "chair", InteractionType = "default" }
                ]
            }
        ]
    };

    private (INetworkClient Client, CatalogPurchaseEventHandler Handler) Create(
        CatalogPageDto page,
        int amount,
        Mock<ICatalogVipPurchaseService>? vipService = null)
    {
        var pageRepository = new Mock<ICatalogPageRepository>();
        pageRepository.SetupGet(x => x.Pages).Returns([page]);

        var state = new Mock<IPlayerState>();

        state.SetupProperty(x => x.LastCatalogPurchase, DateTime.Now.AddDays(-1));

        var playerDto = CatalogClientFactory.MakePlayerDto(1, "buyer");

        var playerLogic = new Mock<IPlayerLogic>();
        playerLogic.SetupGet(x => x.Player).Returns(playerDto);
        playerLogic.SetupGet(x => x.State).Returns(state.Object);

        var client = new Mock<INetworkClient>();
        client.SetupGet(x => x.Player).Returns(playerLogic.Object);
        client.Setup(x => x.WriteToStreamAsync(It.IsAny<AbstractPacketWriter>())).Returns(Task.CompletedTask);

        var handler = new CatalogPurchaseEventHandler(
            pageRepository.Object,
            _chargeService.Object,
            _furnitureService.Object,
            _botService.Object,
            _teleportService.Object,
            _confirmationService.Object,
            (vipService ?? new Mock<ICatalogVipPurchaseService>()).Object,
            NullLogger<CatalogPurchaseEventHandler>.Instance)
        {
            PageId = _pageId,
            ItemId = _itemId,
            Amount = amount
        };

        return (client.Object, handler);
    }
}
