using Ada.Db;
using Ada.Db.Players;
using Ada.Tests.Common;
using Moq;

namespace Ada.Tests.Db;

[TestFixture]
public class PlayerPresenceStoreTests
{
    private SqliteTestDbFactory _dbFactory = null!;

    [SetUp]
    public void SetUp()
    {
        _dbFactory = new SqliteTestDbFactory();

        using var db = _dbFactory.CreateDbContext();

        db.Players.Add(new global::Ada.Db.Models.Players.Player
        {
            Id = 1, Username = "player", Email = "", Password = ""
        });

        db.SaveChanges();
        db.PlayerData.Add(new global::Ada.Db.Models.Players.PlayerData { PlayerId = 1, IsOnline = true, Player = null! });
        db.SaveChanges();
    }

    [TearDown]
    public void TearDown() => _dbFactory.Dispose();

    [Test]
    public async Task SetOfflineAsync_ClearsTheOnlineFlag()
    {
        await new PlayerPresenceStore(_dbFactory).SetOfflineAsync(1);

        await using var db = _dbFactory.CreateDbContext();
        var data = await db.PlayerData.SingleAsync(x => x.PlayerId == 1);

        Assert.That(data.IsOnline, Is.False);
    }

    [Test]
    public async Task SetOfflineAsync_UnknownPlayer_DoesNothing()
    {
        Assert.DoesNotThrowAsync(() => new PlayerPresenceStore(_dbFactory).SetOfflineAsync(999));

        await using var db = _dbFactory.CreateDbContext();
        Assert.That((await db.PlayerData.SingleAsync(x => x.PlayerId == 1)).IsOnline, Is.True);
    }

    [Test]
    public void SetOfflineAsync_ConcurrentRemoval_IsSwallowed()
    {
        var factory = new Mock<IDbContextFactory<AdaDbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Assert.DoesNotThrowAsync(() => new PlayerPresenceStore(factory.Object).SetOfflineAsync(1));
    }
}
