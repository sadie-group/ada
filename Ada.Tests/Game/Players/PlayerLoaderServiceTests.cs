using Ada.API.DTOs.Players;
using Ada.Db.Models.Players;
using Ada.Game.Players;
using Ada.Game.Players.Options;
using Ada.Tests.Common;
using AutoMapper;
using Moq;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace Ada.Tests.Game.Players;

[TestFixture]
public class PlayerLoaderServiceTests
{
    private static IMapper CreateMapper()
    {
        var mapper = new Mock<IMapper>();
        mapper.Setup(m => m.Map<PlayerSsoTokenDto>(It.IsAny<object>()))
            .Returns((object src) =>
            {
                var entity = (PlayerSsoToken)src;
                return new PlayerSsoTokenDto
                {
                    Id = entity.Id,
                    PlayerId = entity.PlayerId,
                    Token = entity.Token,
                    CreatedAt = entity.CreatedAt,
                    ExpiresAt = entity.ExpiresAt,
                    UsedAt = entity.UsedAt
                };
            });
        return mapper.Object;
    }

    private static PlayerLoaderService CreateService(SqliteTestDbFactory factory, bool canReuse) =>
        new(factory, MsOptions.Create(new PlayerOptions { CanReuseSsoTokens = canReuse }), CreateMapper());

    private static async Task SeedTokenAsync(SqliteTestDbFactory factory, string token,
        DateTimeOffset expiresAt, DateTimeOffset? usedAt = null)
    {
        await using var db = factory.CreateDbContext();
        if (db.Players.Count(p => p.Id == 1) == 0)
        {
            db.Players.Add(new Player { Id = 1, Username = "alice", Email = "a@test.com", Password = "secret" });
        }

        db.PlayerSsoToken.Add(new PlayerSsoToken
        {
            PlayerId = 1,
            Token = token,
            CreatedAt = DateTimeOffset.Now.AddMinutes(-5),
            ExpiresAt = expiresAt,
            UsedAt = usedAt
        });
        await db.SaveChangesAsync();
    }

    [Test]
    public async Task GetTokenAsync_UnknownToken_ReturnsNull()
    {
        using var factory = new SqliteTestDbFactory();
        var service = CreateService(factory, canReuse: false);

        Assert.That(await service.GetTokenAsync("missing", 0), Is.Null);
    }

    [Test]
    public async Task GetTokenAsync_ExpiredToken_ReturnsNull()
    {
        using var factory = new SqliteTestDbFactory();
        await SeedTokenAsync(factory, "sso", DateTimeOffset.Now.AddHours(-1));
        var service = CreateService(factory, canReuse: false);

        Assert.That(await service.GetTokenAsync("sso", 0), Is.Null);
    }

    [Test]
    public async Task GetTokenAsync_ExpiredWithinDelayGrace_ReturnsToken()
    {
        using var factory = new SqliteTestDbFactory();
        await SeedTokenAsync(factory, "sso", DateTimeOffset.Now.AddSeconds(-5));
        var service = CreateService(factory, canReuse: true);

        var dto = await service.GetTokenAsync("sso", 60_000);

        Assert.That(dto, Is.Not.Null);
        Assert.That(dto!.Token, Is.EqualTo("sso"));
    }

    [Test]
    public async Task GetTokenAsync_UsedToken_ReturnsNull()
    {
        using var factory = new SqliteTestDbFactory();
        await SeedTokenAsync(factory, "sso", DateTimeOffset.Now.AddHours(1), usedAt: DateTimeOffset.Now.AddMinutes(-1));
        var service = CreateService(factory, canReuse: false);

        Assert.That(await service.GetTokenAsync("sso", 0), Is.Null);
    }

    [Test]
    public async Task GetTokenAsync_Reusable_ReturnsTokenWithoutClaiming()
    {
        using var factory = new SqliteTestDbFactory();
        await SeedTokenAsync(factory, "sso", DateTimeOffset.Now.AddHours(1));
        var service = CreateService(factory, canReuse: true);

        var first = await service.GetTokenAsync("sso", 0);
        var second = await service.GetTokenAsync("sso", 0);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(first!.UsedAt, Is.Null);
        });

        await using var db = factory.CreateDbContext();
        Assert.That(db.PlayerSsoToken.Single(x => x.Token == "sso").UsedAt, Is.Null);
    }

    [Test]
    public async Task GetTokenAsync_SingleUse_ClaimsToken()
    {
        using var factory = new SqliteTestDbFactory();
        await SeedTokenAsync(factory, "sso", DateTimeOffset.Now.AddHours(1));
        var service = CreateService(factory, canReuse: false);

        var first = await service.GetTokenAsync("sso", 0);
        var second = await service.GetTokenAsync("sso", 0);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.Null);
            Assert.That(first!.UsedAt, Is.Not.Null);
            Assert.That(second, Is.Null);
        });

        await using var db = factory.CreateDbContext();
        Assert.That(db.PlayerSsoToken.Single(x => x.Token == "sso").UsedAt, Is.Not.Null);
    }
}
