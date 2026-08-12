using Ada.API.DTOs.Catalog.Items;
using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Client;
using Ada.Core.Shared.Constants;
using Ada.Game.Catalog.Purchase;
using Ada.Tests.Common;
using Moq;

namespace Ada.Tests.Game.Catalog;

[TestFixture]
public class CatalogChargeServiceLimitTests
{
    private SqliteTestDbFactory _dbFactory = null!;
    private CatalogChargeService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _dbFactory = new SqliteTestDbFactory();
        _service = new CatalogChargeService(_dbFactory, NullLogger<CatalogChargeService>.Instance);
    }

    [TearDown]
    public void TearDown() => _dbFactory.Dispose();

    [Test]
    public async Task TryCharge_AmountAboveCap_IsRejected()
    {
        var client = Client(credits: 1_000_000);
        var item = new CatalogItemDto { Id = 1, CostCredits = 1 };

        var charged = await _service.TryChargeAsync(client, item, PurchaseLimits.MaxPurchaseAmount + 1);

        Assert.That(charged, Is.False);
    }

    [Test]
    public async Task TryCharge_FreeItemWithHugeAmount_IsRejected()
    {
        var client = Client(credits: 0);
        var item = new CatalogItemDto { Id = 1, CostCredits = 0, CostPoints = 0 };

        var charged = await _service.TryChargeAsync(client, item, int.MaxValue);

        Assert.That(charged, Is.False);
    }

    [Test]
    public async Task TryCharge_FreeItemWithinCap_IsAllowed()
    {
        var client = Client(credits: 0);
        var item = new CatalogItemDto { Id = 1, CostCredits = 0, CostPoints = 0 };

        var charged = await _service.TryChargeAsync(client, item, 1);

        Assert.That(charged, Is.True);
    }

    [Test]
    public async Task TryCharge_AmountAtCapWithFunds_IsAllowed()
    {
        await SeedPlayerDataAsync(credits: 1_000);

        var client = Client(credits: 1_000);
        var item = new CatalogItemDto { Id = 1, CostCredits = 1 };

        var charged = await _service.TryChargeAsync(client, item, PurchaseLimits.MaxPurchaseAmount);

        Assert.That(charged, Is.True);
    }

    private async Task SeedPlayerDataAsync(int credits)
    {
        await using var db = _dbFactory.CreateDbContext();
        var player = new Ada.Db.Models.Players.Player
        {
            Id = 1, Username = "buyer", Email = "b@test.com", Password = "secret"
        };
        db.Players.Add(player);
        db.PlayerData.Add(new Ada.Db.Models.Players.PlayerData
        {
            Player = player, PlayerId = 1, CreditBalance = credits
        });
        await db.SaveChangesAsync();
    }

    private static INetworkClient Client(int credits)
    {
        var data = new PlayerDataDto { CreditBalance = credits };
        var player = new PlayerDto(
            1, "buyer", "", DateTimeOffset.UtcNow, [], data, new PlayerAvatarDataDto(), [], [], [], [],
            new PlayerNavigatorSettingsDto(), new PlayerGameSettingsDto(), [], [], [], [], [], [], [], [], [], [],
            [], [], [], [], [], [], []);

        var logic = new Mock<IPlayerLogic>();
        logic.SetupGet(x => x.Player).Returns(player);

        var client = new Mock<INetworkClient>();
        client.SetupGet(x => x.Player).Returns(logic.Object);
        client.Setup(x => x.WriteToStreamAsync(It.IsAny<AbstractPacketWriter>())).Returns(Task.CompletedTask);

        return client.Object;
    }
}
