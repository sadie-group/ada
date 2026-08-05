using Ada.API;
using Ada.API.DTOs.Catalog.Items;
using Ada.API.DTOs.Furniture;
using Ada.API.DTOs.Players;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Catalog;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Client;
using Ada.Core.Enums.Game.Catalog;
using Ada.Db.Models.Catalog.Items;
using Ada.Db.Models.Players.Furniture;
using Ada.Game.Catalog.Purchase;
using Ada.Networking.Writers.Catalog;
using Ada.Networking.Writers.Players;
using Ada.Networking.Writers.Players.Inventory;
using Ada.Tests.Common;
using AutoMapper;
using Moq;

namespace Ada.Tests.Game.Catalog;

internal static class PurchaseTestHelpers
{
    public static PlayerDto MakePlayer(long id, List<PlayerFurnitureItemDto> furniture, List<PlayerBotDto> bots)
    {
        return new PlayerDto(
            id,
            "buyer",
            "buyer@test.com",
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
            furniture,
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
            bots,
            [],
            [],
            []);
    }

    public static (Mock<INetworkClient> Client, List<AbstractPacketWriter> Written) MakeClient(PlayerDto? player)
    {
        var written = new List<AbstractPacketWriter>();
        var client = new Mock<INetworkClient>();

        if (player != null)
        {
            var logic = new Mock<IPlayerLogic>();
            logic.SetupGet(x => x.Player).Returns(player);
            client.SetupGet(x => x.Player).Returns(logic.Object);
        }

        client.Setup(x => x.WriteToStreamAsync(It.IsAny<AbstractPacketWriter>()))
            .Callback((AbstractPacketWriter w) => written.Add(w))
            .Returns(Task.CompletedTask);

        return (client, written);
    }
}

[TestFixture]
public class CatalogTeleportPurchaseServiceTests
{
    private static readonly FurnitureItemDto Teleport = new()
    {
        Id = 5,
        Name = "teleport",
        AssetName = "teleport",
        InteractionType = "teleport"
    };

    private static IMapper CreateMapper(List<PlayerFurnitureItem> created)
    {
        var mapper = new Mock<IMapper>();

        mapper.Setup(m => m.Map<FurnitureItemDto>(It.IsAny<object>())).Returns(Teleport);

        mapper.Setup(m => m.Map<PlayerFurnitureItem>(It.IsAny<object>()))
            .Returns((object src) =>
            {
                var dto = (PlayerFurnitureItemDto)src;
                var entity = new PlayerFurnitureItem
                {
                    Player = null!,
                    FurnitureItem = null!,
                    FurnitureItemId = dto.FurnitureItemId,
                    PlayerId = dto.PlayerId,
                    LimitedData = dto.LimitedData,
                    MetaData = dto.MetaData,
                    CreatedAt = dto.CreatedAt
                };
                created.Add(entity);
                return entity;
            });

        mapper.Setup(m => m.Map<PlayerFurnitureItemDto>(It.IsAny<object>()))
            .Returns((object src) =>
            {
                var entity = (PlayerFurnitureItem)src;
                return new PlayerFurnitureItemDto
                {
                    Id = entity.Id,
                    PlayerId = entity.PlayerId,
                    FurnitureItem = Teleport,
                    FurnitureItemId = entity.FurnitureItemId,
                    LimitedData = entity.LimitedData,
                    MetaData = entity.MetaData,
                    CreatedAt = entity.CreatedAt
                };
            });

        return mapper.Object;
    }

    [Test]
    public async Task ProcessAsync_PersistsLinkedPairWritesInventoryAndConfirms()
    {
        var factory = TestDbFactory.CreateDbFactory();
        var created = new List<PlayerFurnitureItem>();
        var furniture = new List<PlayerFurnitureItemDto>();
        var player = PurchaseTestHelpers.MakePlayer(7, furniture, []);
        var (client, written) = PurchaseTestHelpers.MakeClient(player);
        var confirmation = new Mock<ICatalogPurchaseConfirmationService>();
        var item = new CatalogItemDto { Id = 3, Name = "tele", FurnitureItems = [Teleport] };
        var service = new CatalogTeleportPurchaseService(factory, confirmation.Object, CreateMapper(created),
            NullLogger<CatalogTeleportPurchaseService>.Instance);

        await service.ProcessAsync(client.Object, item, "42", 1);

        Assert.That(created, Has.Count.EqualTo(2));
        Assert.That(created[0].Id, Is.Not.EqualTo(created[1].Id));

        using var db = factory.CreateDbContext();
        Assert.That(db.PlayerFurnitureItems.Count(), Is.EqualTo(2));

        var link = db.PlayerFurnitureItemLinks.Single();
        Assert.That(link.ParentId, Is.EqualTo(created[0].Id));
        Assert.That(link.ChildId, Is.EqualTo(created[1].Id));

        Assert.That(furniture.Select(x => x.Id), Is.EquivalentTo(new[] { created[0].Id, created[1].Id }));
        Assert.That(furniture.Select(x => x.MetaData), Is.All.EqualTo("42"));

        var unseen = (PlayerInventoryUnseenItemsWriter)written.Single();
        Assert.That(unseen.Count, Is.EqualTo(2));
        Assert.That(unseen.Category, Is.EqualTo(1));
        Assert.That(unseen.FurnitureItems, Has.Count.EqualTo(2));

        confirmation.Verify(x => x.ConfirmAsync(client.Object, item, 1), Times.Once);
    }

    [Test]
    public async Task ProcessAsync_NullMetaData_DefaultsToEmpty()
    {
        var factory = TestDbFactory.CreateDbFactory();
        var created = new List<PlayerFurnitureItem>();
        var player = PurchaseTestHelpers.MakePlayer(7, [], []);
        var (client, _) = PurchaseTestHelpers.MakeClient(player);
        var service = new CatalogTeleportPurchaseService(
            factory, Mock.Of<ICatalogPurchaseConfirmationService>(), CreateMapper(created),
            NullLogger<CatalogTeleportPurchaseService>.Instance);

        await service.ProcessAsync(client.Object, new CatalogItemDto { Id = 3, FurnitureItems = [Teleport] }, null, 1);

        Assert.That(created, Has.Count.EqualTo(2));
        Assert.That(created.Select(x => x.MetaData), Is.All.EqualTo(""));
    }
}

[TestFixture]
public class CatalogVipPurchaseServiceTests
{
    [Test]
    public async Task ProcessAsync_NullPlayer_WritesNothing()
    {
        var factory = TestDbFactory.CreateDbFactory();
        var (client, written) = PurchaseTestHelpers.MakeClient(null);
        var service = new CatalogVipPurchaseService(factory);

        await service.ProcessAsync(client.Object, 1);

        Assert.That(written, Is.Empty);
    }

    [Test]
    public async Task ProcessAsync_UnknownItem_WritesFailure()
    {
        var factory = TestDbFactory.CreateDbFactory();
        var (client, written) = PurchaseTestHelpers.MakeClient(PurchaseTestHelpers.MakePlayer(1, [], []));
        var service = new CatalogVipPurchaseService(factory);

        await service.ProcessAsync(client.Object, 404);

        var failed = (CatalogPurchaseFailedWriter)written.Single();
        Assert.That(failed.Error, Is.EqualTo((int)CatalogPurchaseError.Server));
    }

    [Test]
    public async Task ProcessAsync_KnownItem_WritesOkAndRefresh()
    {
        var factory = TestDbFactory.CreateDbFactory();
        using (var db = factory.CreateDbContext())
        {
            db.CatalogItems.Add(new CatalogItem
            {
                Id = 9,
                Name = "vip",
                CostCredits = 25,
                CostPoints = 5,
                CostPointsType = 105,
                MetaData = "meta"
            });
            db.SaveChanges();
        }

        var (client, written) = PurchaseTestHelpers.MakeClient(PurchaseTestHelpers.MakePlayer(1, [], []));
        var service = new CatalogVipPurchaseService(factory);

        await service.ProcessAsync(client.Object, 9);

        Assert.That(written, Has.Count.EqualTo(2));

        var ok = (CatalogPurchaseOkWriter)written[0];
        Assert.That(ok.Id, Is.EqualTo(9));
        Assert.That(ok.Name, Is.EqualTo("vip"));
        Assert.That(ok.Rented, Is.False);
        Assert.That(ok.CostCredits, Is.EqualTo(25));
        Assert.That(ok.CostPoints, Is.EqualTo(5));
        Assert.That(ok.CostPointsType, Is.EqualTo(105));
        Assert.That(ok.ClubLevel, Is.EqualTo(1));
        Assert.That(ok.Metadata, Is.EqualTo("meta"));
        Assert.That(ok.IsLimited, Is.False);

        Assert.That(written[1], Is.InstanceOf<PlayerInventoryRefreshWriter>());
    }
}

[TestFixture]
public class CatalogBotPurchaseServiceTests
{
    private const string BotData = "name:Bobba;figure:hr-100;motto:beep;gender:m";

    [TestCase("")]
    [TestCase(null)]
    public async Task ProcessAsync_MissingMetaData_DoesNothing(string? metaData)
    {
        var factory = TestDbFactory.CreateDbFactory();
        var confirmation = new Mock<ICatalogPurchaseConfirmationService>();
        var (client, written) = PurchaseTestHelpers.MakeClient(PurchaseTestHelpers.MakePlayer(1, [], []));
        var service = new CatalogBotPurchaseService(factory, confirmation.Object,
            NullLogger<CatalogBotPurchaseService>.Instance);

        await service.ProcessAsync(client.Object, new CatalogItemDto { Id = 1, MetaData = metaData });

        Assert.That(written, Is.Empty);
        confirmation.Verify(
            x => x.ConfirmAsync(It.IsAny<INetworkClient>(), It.IsAny<CatalogItemDto>(), It.IsAny<int>()),
            Times.Never);
    }

    [Test]
    public async Task ProcessAsync_NullPlayer_DoesNothing()
    {
        var factory = TestDbFactory.CreateDbFactory();
        var confirmation = new Mock<ICatalogPurchaseConfirmationService>();
        var (client, written) = PurchaseTestHelpers.MakeClient(null);
        var service = new CatalogBotPurchaseService(factory, confirmation.Object,
            NullLogger<CatalogBotPurchaseService>.Instance);

        await service.ProcessAsync(client.Object, new CatalogItemDto { Id = 1, MetaData = BotData });

        Assert.That(written, Is.Empty);
        confirmation.Verify(
            x => x.ConfirmAsync(It.IsAny<INetworkClient>(), It.IsAny<CatalogItemDto>(), It.IsAny<int>()),
            Times.Never);
    }

    [TestCase("gender:m")]
    [TestCase("gender:F")]
    public void ProcessAsync_BotDtoNotInEfModel_Throws(string gender)
    {
        var factory = TestDbFactory.CreateDbFactory();
        var (client, written) = PurchaseTestHelpers.MakeClient(PurchaseTestHelpers.MakePlayer(1, [], []));
        var service = new CatalogBotPurchaseService(factory, Mock.Of<ICatalogPurchaseConfirmationService>(),
            NullLogger<CatalogBotPurchaseService>.Instance);
        var item = new CatalogItemDto { Id = 1, MetaData = $"name:Bobba;figure:hr-100;motto:beep;{gender}" };

        Assert.ThrowsAsync<InvalidOperationException>(() => service.ProcessAsync(client.Object, item));
        Assert.That(written, Is.Empty);
    }
}
