using Ada.API.DTOs;
using Ada.API.DTOs.Catalog.Items;
using Ada.API.DTOs.Furniture;
using Ada.API.DTOs.Players;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.DTOs.Server;
using Ada.API.Interfaces.Game.Catalog;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.WordFilter;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Client;
using Ada.Core.Enums.Game.Catalog;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Enums.Game.WordFilter;
using Ada.Db;
using Ada.Db.Models.Players;
using Ada.Db.Models.Players.Furniture;
using Ada.Game.Catalog.Purchase;
using Ada.Networking.Writers.Catalog;
using Ada.Networking.Writers.Players;
using Ada.Networking.Writers.Players.Inventory;
using Ada.Networking.Writers.Players.Purse;
using Ada.Tests.Common;
using AutoMapper;
using Moq;

namespace Ada.Tests.Game.Catalog;

internal static class CatalogClientFactory
{
    public static PlayerDto MakePlayerDto(
        long id,
        string username,
        PlayerDataDto? data = null,
        ICollection<PlayerSubscriptionDto>? subscriptions = null,
        ICollection<PlayerFurnitureItemDto>? furnitureItems = null)
    {
        return new PlayerDto(
            id,
            username,
            "test@example.com",
            DateTimeOffset.UtcNow,
            [],
            data,
            new PlayerAvatarDataDto { FigureCode = "figure", Motto = "motto" },
            [],
            [],
            [],
            [],
            new PlayerNavigatorSettingsDto(),
            new PlayerGameSettingsDto(),
            [],
            furnitureItems ?? [],
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

    public static (Mock<INetworkClient> Client, List<AbstractPacketWriter> Written) MakeClient(PlayerDto? player)
    {
        var written = new List<AbstractPacketWriter>();

        var client = new Mock<INetworkClient>();
        client.Setup(x => x.WriteToStreamAsync(It.IsAny<AbstractPacketWriter>()))
            .Callback<AbstractPacketWriter>(written.Add)
            .Returns(Task.CompletedTask);

        if (player != null)
        {
            var logic = new Mock<IPlayerLogic>();
            logic.SetupGet(x => x.Player).Returns(player);
            client.SetupGet(x => x.Player).Returns(logic.Object);
        }

        return (client, written);
    }
}

[TestFixture]
public class CatalogChargeServiceTests
{
    private static CatalogItemDto MakeItem(
        int costCredits = 0, int costPoints = 0, int costPointsType = 0, bool requiresClub = false)
    {
        return new CatalogItemDto
        {
            Id = 1,
            Name = "item",
            CostCredits = costCredits,
            CostPoints = costPoints,
            CostPointsType = costPointsType,
            RequiresClubMembership = requiresClub
        };
    }

    private static PlayerDataDto MakeData() => new()
    {
        PlayerId = 1,
        CreditBalance = 100,
        PixelBalance = 50,
        SeasonalBalance = 25,
        GotwPoints = 5
    };

    private static async Task<SqliteTestDbFactory> SeedAsync()
    {
        var factory = new SqliteTestDbFactory();

        await using var db = factory.CreateDbContext();
        var player = new Player { Id = 1, Username = "buyer", Email = "b@test.com", Password = "secret" };
        db.Players.Add(player);
        db.PlayerData.Add(new PlayerData
        {
            Player = player, PlayerId = 1, CreditBalance = 100, PixelBalance = 50, SeasonalBalance = 25, GotwPoints = 5
        });
        await db.SaveChangesAsync();

        return factory;
    }

    [Test]
    public void HasRequiredMembership_NoClubRequired_ReturnsTrue()
    {
        var service = new CatalogChargeService(TestDbFactory.CreateDbFactory(), NullLogger<CatalogChargeService>.Instance);
        var (client, _) = CatalogClientFactory.MakeClient(null);

        Assert.That(service.HasRequiredMembership(client.Object, MakeItem()), Is.True);
    }

    [Test]
    public void HasRequiredMembership_ClubRequiredWithoutPlayer_ReturnsFalse()
    {
        var service = new CatalogChargeService(TestDbFactory.CreateDbFactory(), NullLogger<CatalogChargeService>.Instance);
        var (client, _) = CatalogClientFactory.MakeClient(null);

        Assert.That(service.HasRequiredMembership(client.Object, MakeItem(requiresClub: true)), Is.False);
    }

    [Test]
    public void HasRequiredMembership_WithHabboClub_ReturnsTrue()
    {
        var service = new CatalogChargeService(TestDbFactory.CreateDbFactory(), NullLogger<CatalogChargeService>.Instance);
        var player = CatalogClientFactory.MakePlayerDto(1, "buyer", subscriptions:
        [
            new PlayerSubscriptionDto { Subscription = new SubscriptionDto { Name = "HABBO_CLUB" } }
        ]);
        var (client, _) = CatalogClientFactory.MakeClient(player);

        Assert.That(service.HasRequiredMembership(client.Object, MakeItem(requiresClub: true)), Is.True);
    }

    [Test]
    public void HasRequiredMembership_WithoutHabboClub_ReturnsFalse()
    {
        var service = new CatalogChargeService(TestDbFactory.CreateDbFactory(), NullLogger<CatalogChargeService>.Instance);
        var player = CatalogClientFactory.MakePlayerDto(1, "buyer", subscriptions:
        [
            new PlayerSubscriptionDto(),
            new PlayerSubscriptionDto { Subscription = new SubscriptionDto { Name = "OTHER" } }
        ]);
        var (client, _) = CatalogClientFactory.MakeClient(player);

        Assert.That(service.HasRequiredMembership(client.Object, MakeItem(requiresClub: true)), Is.False);
    }

    [Test]
    public async Task TryChargeAsync_InsufficientCredits_ReturnsFalse()
    {
        var service = new CatalogChargeService(TestDbFactory.CreateDbFactory(), NullLogger<CatalogChargeService>.Instance);
        var data = MakeData();
        var (client, written) = CatalogClientFactory.MakeClient(CatalogClientFactory.MakePlayerDto(1, "buyer", data));

        var result = await service.TryChargeAsync(client.Object, MakeItem(costCredits: 60), 2);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(data.CreditBalance, Is.EqualTo(100));
            Assert.That(written, Is.Empty);
        });
    }

    [Test]
    public async Task TryChargeAsync_InsufficientPixels_ReturnsFalse()
    {
        var service = new CatalogChargeService(TestDbFactory.CreateDbFactory(), NullLogger<CatalogChargeService>.Instance);
        var data = MakeData();
        var (client, written) = CatalogClientFactory.MakeClient(CatalogClientFactory.MakePlayerDto(1, "buyer", data));

        var result = await service.TryChargeAsync(client.Object, MakeItem(costPoints: 60), 1);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(data.PixelBalance, Is.EqualTo(50));
            Assert.That(written, Is.Empty);
        });
    }

    [Test]
    public async Task TryChargeAsync_InsufficientSeasonal_ReturnsFalse()
    {
        var service = new CatalogChargeService(TestDbFactory.CreateDbFactory(), NullLogger<CatalogChargeService>.Instance);
        var data = MakeData();
        var (client, written) = CatalogClientFactory.MakeClient(CatalogClientFactory.MakePlayerDto(1, "buyer", data));

        var result = await service.TryChargeAsync(client.Object, MakeItem(costPoints: 30, costPointsType: 1), 1);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(data.SeasonalBalance, Is.EqualTo(25));
            Assert.That(written, Is.Empty);
        });
    }

    [Test]
    public async Task TryChargeAsync_FreeItem_ReturnsTrueWithoutWrites()
    {
        var service = new CatalogChargeService(TestDbFactory.CreateDbFactory(), NullLogger<CatalogChargeService>.Instance);
        var data = MakeData();
        var (client, written) = CatalogClientFactory.MakeClient(CatalogClientFactory.MakePlayerDto(1, "buyer", data));

        var result = await service.TryChargeAsync(client.Object, MakeItem(), 1);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(written, Is.Empty);
            Assert.That(data.CreditBalance, Is.EqualTo(100));
        });
    }

    [Test]
    public async Task TryChargeAsync_Credits_DeductsWritesAndPersists()
    {
        using var factory = await SeedAsync();
        var service = new CatalogChargeService(factory, NullLogger<CatalogChargeService>.Instance);
        var data = MakeData();
        var (client, written) = CatalogClientFactory.MakeClient(CatalogClientFactory.MakePlayerDto(1, "buyer", data));

        var result = await service.TryChargeAsync(client.Object, MakeItem(costCredits: 30), 2);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(data.CreditBalance, Is.EqualTo(40));
            Assert.That(written, Has.Count.EqualTo(1));
            Assert.That(((PlayerCreditsBalanceWriter)written[0]).Credits, Is.EqualTo(40));
        });
        await using var db = factory.CreateDbContext();
        var row = db.PlayerData.Single(x => x.PlayerId == 1);
        Assert.Multiple(() =>
        {
            Assert.That(row.CreditBalance, Is.EqualTo(40));
            Assert.That(row.PixelBalance, Is.EqualTo(50));
            Assert.That(row.SeasonalBalance, Is.EqualTo(25));
        });
    }

    [Test]
    public async Task TryChargeAsync_PixelPoints_DeductsWritesAndPersists()
    {
        using var factory = await SeedAsync();
        var service = new CatalogChargeService(factory, NullLogger<CatalogChargeService>.Instance);
        var data = MakeData();
        var (client, written) = CatalogClientFactory.MakeClient(CatalogClientFactory.MakePlayerDto(1, "buyer", data));

        var result = await service.TryChargeAsync(client.Object, MakeItem(costPoints: 10), 1);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(data.PixelBalance, Is.EqualTo(40));
            Assert.That(written, Has.Count.EqualTo(1));
        });
        var currencies = ((PlayerActivityPointsBalanceWriter)written[0]).Currencies;
        Assert.Multiple(() =>
        {
            Assert.That(currencies[0], Is.EqualTo(40));
            Assert.That(currencies[5], Is.EqualTo(25));
            Assert.That(currencies[103], Is.EqualTo(5));
        });
        await using var db = factory.CreateDbContext();
        Assert.That(db.PlayerData.Single(x => x.PlayerId == 1).PixelBalance, Is.EqualTo(40));
    }

    [Test]
    public async Task TryChargeAsync_SeasonalPoints_DeductsWritesAndPersists()
    {
        using var factory = await SeedAsync();
        var service = new CatalogChargeService(factory, NullLogger<CatalogChargeService>.Instance);
        var data = MakeData();
        var (client, written) = CatalogClientFactory.MakeClient(CatalogClientFactory.MakePlayerDto(1, "buyer", data));

        var result = await service.TryChargeAsync(client.Object, MakeItem(costPoints: 10, costPointsType: 5), 1);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(data.SeasonalBalance, Is.EqualTo(15));
            Assert.That(data.PixelBalance, Is.EqualTo(50));
            Assert.That(((PlayerActivityPointsBalanceWriter)written[0]).Currencies[5], Is.EqualTo(15));
        });
        await using var db = factory.CreateDbContext();
        Assert.That(db.PlayerData.Single(x => x.PlayerId == 1).SeasonalBalance, Is.EqualTo(15));
    }

    [Test]
    public async Task TryChargeAsync_CreditsAndPoints_WritesBoth()
    {
        using var factory = await SeedAsync();
        var service = new CatalogChargeService(factory, NullLogger<CatalogChargeService>.Instance);
        var data = MakeData();
        var (client, written) = CatalogClientFactory.MakeClient(CatalogClientFactory.MakePlayerDto(1, "buyer", data));

        var result = await service.TryChargeAsync(client.Object, MakeItem(costCredits: 10, costPoints: 5), 1);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(written, Has.Count.EqualTo(2));
        });
        Assert.Multiple(() =>
        {
            Assert.That(written[0], Is.TypeOf<PlayerCreditsBalanceWriter>());
            Assert.That(written[1], Is.TypeOf<PlayerActivityPointsBalanceWriter>());
        });
        await using var db = factory.CreateDbContext();
        var row = db.PlayerData.Single(x => x.PlayerId == 1);
        Assert.Multiple(() =>
        {
            Assert.That(row.CreditBalance, Is.EqualTo(90));
            Assert.That(row.PixelBalance, Is.EqualTo(45));
        });
    }
}

[TestFixture]
public class CatalogFurniturePurchaseServiceTests
{
    private static FurnitureItemDto MakeFurniture(string? interactionType = "default") => new()
    {
        Id = 7,
        Name = "chair",
        AssetName = "chair",
        InteractionType = interactionType,
        CanGift = true
    };

    private static CatalogItemDto MakeItem(FurnitureItemDto furniture) => new()
    {
        Id = 3,
        Name = "deal",
        FurnitureItems = [furniture]
    };

    private static (CatalogFurniturePurchaseService Service,
        Mock<ICatalogPurchaseConfirmationService> Confirmation,
        Mock<IWordFilterService> WordFilter,
        IDbContextFactory<AdaDbContext> Factory)
        MakeService(FurnitureItemDto furniture)
    {
        var factory = TestDbFactory.CreateDbFactory();
        var confirmation = new Mock<ICatalogPurchaseConfirmationService>();

        var wordFilter = new Mock<IWordFilterService>();
        wordFilter.Setup(x => x.Filter(It.IsAny<string>(), It.IsAny<WordFilterContext>()))
            .Returns((string text, WordFilterContext _) => new WordFilterResultDto
            {
                OriginalText = text,
                FilteredText = "filtered"
            });

        var entityPlayer = new Player { Id = 1, Username = "buyer", Email = "b@test.com", Password = "secret" };
        var entityFurniture = new Ada.Db.Models.Furniture.FurnitureItem
        {
            Name = "chair",
            AssetName = "chair",
            InteractionType = null
        };

        var nextId = 0;
        var mapper = new Mock<IMapper>();
        mapper.Setup(x => x.Map<FurnitureItemDto>(It.IsAny<object>())).Returns(furniture);
        mapper.Setup(x => x.Map<PlayerFurnitureItem>(It.IsAny<object>()))
            .Returns(() => new PlayerFurnitureItem
            {
                Id = ++nextId,
                Player = entityPlayer,
                FurnitureItemId = 7,
                FurnitureItem = entityFurniture,
                LimitedData = "1:1",
                MetaData = ""
            });

        var service = new CatalogFurniturePurchaseService(factory, confirmation.Object, wordFilter.Object, mapper.Object,
            NullLogger<CatalogFurniturePurchaseService>.Instance);
        return (service, confirmation, wordFilter, factory);
    }

    [Test]
    public async Task ProcessAsync_AddsItemsPersistsAndConfirms()
    {
        var furniture = MakeFurniture();
        var (service, confirmation, wordFilter, factory) = MakeService(furniture);
        var item = MakeItem(furniture);
        var inventory = new List<PlayerFurnitureItemDto>();
        var (client, written) = CatalogClientFactory.MakeClient(
            CatalogClientFactory.MakePlayerDto(1, "buyer", furnitureItems: inventory));

        await service.ProcessAsync(client.Object, item, null, 3);

        Assert.That(inventory, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(inventory[0].PlayerId, Is.EqualTo(1));
            Assert.That(inventory[0].FurnitureItemId, Is.EqualTo(7));
            Assert.That(inventory[0].LimitedData, Is.EqualTo("1:1"));
            Assert.That(inventory[0].MetaData, Is.EqualTo(""));
        });
        await using var db = await factory.CreateDbContextAsync();
        Assert.That(db.PlayerFurnitureItems.Count(), Is.EqualTo(3));

        var unseen = (PlayerInventoryUnseenItemsWriter)written.Single();
        Assert.That(unseen.Count, Is.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(unseen.Category, Is.EqualTo(1));
            Assert.That(unseen.FurnitureItems, Is.EqualTo(inventory));
        });
        confirmation.Verify(x => x.ConfirmAsync(client.Object, item, 3), Times.Once);
        wordFilter.Verify(x => x.Filter(It.IsAny<string>(), It.IsAny<WordFilterContext>()), Times.Never);
    }

    [Test]
    public async Task ProcessAsync_NonTrophy_FiltersMetaData()
    {
        var furniture = MakeFurniture();
        var (service, _, wordFilter, _) = MakeService(furniture);
        var inventory = new List<PlayerFurnitureItemDto>();
        var (client, _) = CatalogClientFactory.MakeClient(
            CatalogClientFactory.MakePlayerDto(1, "buyer", furnitureItems: inventory));

        await service.ProcessAsync(client.Object, MakeItem(furniture), "hello", 1);

        Assert.That(inventory.Single().MetaData, Is.EqualTo("filtered"));
        wordFilter.Verify(x => x.Filter("hello", WordFilterContext.Chat), Times.Once);
    }

    [Test]
    public async Task ProcessAsync_NonTrophy_CapsMetaDataLength()
    {
        var furniture = MakeFurniture();
        var (service, _, wordFilter, _) = MakeService(furniture);
        var inventory = new List<PlayerFurnitureItemDto>();
        var (client, _) = CatalogClientFactory.MakeClient(
            CatalogClientFactory.MakePlayerDto(1, "buyer", furnitureItems: inventory));

        await service.ProcessAsync(client.Object, MakeItem(furniture), new string('a', 5_000), 1);

        wordFilter.Verify(x => x.Filter(It.Is<string>(s => s.Length == 300), WordFilterContext.Chat), Times.Once);
    }

    [Test]
    public async Task ProcessAsync_NonTrophy_EmptyMetaData_SkipsTheFilter()
    {
        var furniture = MakeFurniture();
        var (service, _, wordFilter, _) = MakeService(furniture);
        var inventory = new List<PlayerFurnitureItemDto>();
        var (client, _) = CatalogClientFactory.MakeClient(
            CatalogClientFactory.MakePlayerDto(1, "buyer", furnitureItems: inventory));

        await service.ProcessAsync(client.Object, MakeItem(furniture), null, 1);

        Assert.That(inventory.Single().MetaData, Is.EqualTo(""));
        wordFilter.Verify(x => x.Filter(It.IsAny<string>(), It.IsAny<WordFilterContext>()), Times.Never);
    }

    [Test]
    public void ProcessAsync_AmountOutsideTheCap_Throws()
    {
        var furniture = MakeFurniture();
        var (service, _, _, _) = MakeService(furniture);
        var (client, _) = CatalogClientFactory.MakeClient(
            CatalogClientFactory.MakePlayerDto(1, "buyer", furnitureItems: []));

        Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.ProcessAsync(client.Object, MakeItem(furniture), null, 10_000));
    }

    [Test]
    public async Task ProcessAsync_Trophy_BuildsEngraving()
    {
        var furniture = MakeFurniture(FurnitureItemInteractionType.Trophy);
        var (service, _, wordFilter, _) = MakeService(furniture);
        var inventory = new List<PlayerFurnitureItemDto>();
        var (client, _) = CatalogClientFactory.MakeClient(
            CatalogClientFactory.MakePlayerDto(1, "buyer", furnitureItems: inventory));

        string? filterInput = null;
        wordFilter.Setup(x => x.Filter(It.IsAny<string>(), WordFilterContext.Chat))
            .Callback((string text, WordFilterContext _) => filterInput = text)
            .Returns((string text, WordFilterContext _) => new WordFilterResultDto
            {
                OriginalText = text,
                FilteredText = "filtered"
            });

        var message = new string('a', 295) + "\t" + new string('b', 10);
        await service.ProcessAsync(client.Object, MakeItem(furniture), message, 1);

        Assert.That(filterInput, Has.Length.EqualTo(300));
        Assert.That(filterInput, Does.Not.Contain("\t"));
        Assert.Multiple(() =>
        {
            Assert.That(filterInput, Does.EndWith("bbbbb"));
            Assert.That(inventory.Single().MetaData, Is.EqualTo($"buyer\t{DateTime.Now:d-M-yyyy}\tfiltered"));
        });
    }

    [Test]
    public async Task ProcessAsync_TrophyWithoutMessage_FiltersEmpty()
    {
        var furniture = MakeFurniture(FurnitureItemInteractionType.Trophy);
        var (service, _, wordFilter, _) = MakeService(furniture);
        var inventory = new List<PlayerFurnitureItemDto>();
        var (client, _) = CatalogClientFactory.MakeClient(
            CatalogClientFactory.MakePlayerDto(1, "buyer", furnitureItems: inventory));

        await service.ProcessAsync(client.Object, MakeItem(furniture), null, 1);

        wordFilter.Verify(x => x.Filter("", WordFilterContext.Chat), Times.Once);
        Assert.That(inventory.Single().MetaData, Is.EqualTo($"buyer\t{DateTime.Now:d-M-yyyy}\tfiltered"));
    }
}

[TestFixture]
public class CatalogPurchaseConfirmationServiceTests
{
    private static (CatalogPurchaseConfirmationService Service, List<FurnitureItemDto> Mapped) MakeService()
    {
        var mapped = new List<FurnitureItemDto>
        {
            new() { Id = 7, Name = "chair", AssetName = "chair", InteractionType = null }
        };

        var mapper = new Mock<IMapper>();
        mapper.Setup(x => x.Map<List<FurnitureItemDto>>(It.IsAny<object>())).Returns(mapped);

        return (new CatalogPurchaseConfirmationService(mapper.Object), mapped);
    }

    private static CatalogItemDto MakeItem(bool requiresClub, int amount) => new()
    {
        Id = 3,
        Name = "deal",
        CostCredits = 10,
        CostPoints = 5,
        CostPointsType = 1,
        MetaData = "meta",
        Amount = amount,
        RequiresClubMembership = requiresClub,
        FurnitureItems = [new FurnitureItemDto { Id = 7, Name = "chair", AssetName = "chair", InteractionType = null, CanGift = true }]
    };

    [Test]
    public async Task ConfirmAsync_ClubItem_WritesOkAndRefresh()
    {
        var (service, mapped) = MakeService();
        var (client, written) = CatalogClientFactory.MakeClient(null);

        await service.ConfirmAsync(client.Object, MakeItem(requiresClub: true, amount: 1), 2);

        Assert.That(written, Has.Count.EqualTo(2));

        var ok = (CatalogPurchaseOkWriter)written[0];
        Assert.Multiple(() =>
        {
            Assert.That(ok.Id, Is.EqualTo(3));
            Assert.That(ok.Name, Is.EqualTo("deal"));
            Assert.That(ok.Rented, Is.False);
            Assert.That(ok.CostCredits, Is.EqualTo(10));
            Assert.That(ok.CostPoints, Is.EqualTo(5));
            Assert.That(ok.CostPointsType, Is.EqualTo(1));
            Assert.That(ok.CanGift, Is.True);
            Assert.That(ok.FurnitureItems, Is.EqualTo(mapped));
            Assert.That(ok.Amount, Is.EqualTo(2));
            Assert.That(ok.ClubLevel, Is.EqualTo(1));
            Assert.That(ok.CanPurchaseBundles, Is.False);
            Assert.That(ok.Metadata, Is.EqualTo("meta"));
            Assert.That(ok.IsLimited, Is.False);
            Assert.That(written[1], Is.TypeOf<PlayerInventoryRefreshWriter>());
        });
    }

    [Test]
    public async Task ConfirmAsync_NonClubBundle_SetsLevelAndBundles()
    {
        var (service, _) = MakeService();
        var (client, written) = CatalogClientFactory.MakeClient(null);

        await service.ConfirmAsync(client.Object, MakeItem(requiresClub: false, amount: 5), 1);

        var ok = (CatalogPurchaseOkWriter)written[0];
        Assert.Multiple(() =>
        {
            Assert.That(ok.ClubLevel, Is.EqualTo(0));
            Assert.That(ok.CanPurchaseBundles, Is.True);
        });
    }

    [Test]
    public async Task WriteFailureAsync_WritesServerError()
    {
        var (service, _) = MakeService();
        var (client, written) = CatalogClientFactory.MakeClient(null);

        await service.WriteFailureAsync(client.Object);

        Assert.That(((CatalogPurchaseFailedWriter)written.Single()).Error, Is.EqualTo((int)CatalogPurchaseError.Server));
    }

    [Test]
    public async Task WriteUnavailableAsync_WritesCode()
    {
        var (service, _) = MakeService();
        var (client, written) = CatalogClientFactory.MakeClient(null);

        await service.WriteUnavailableAsync(client.Object);

        Assert.That(((CatalogPurchaseUnavailableWriter)written.Single()).Code, Is.EqualTo(1));
    }
}
