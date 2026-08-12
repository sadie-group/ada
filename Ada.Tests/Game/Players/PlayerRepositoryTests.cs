using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking;
using Ada.Db.Models.Players;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Constants;
using Ada.Game.Players;
using Ada.Tests.Common;
using AutoMapper;
using Moq;

namespace Ada.Tests.Game.Players;

[TestFixture]
public class PlayerRepositoryTests
{
    private static PlayerDto CreateDto(long id, string username) => new(
        id, username, "mail@test.com", DateTimeOffset.UnixEpoch,
        [], null, null, [], [], [], [], null, null, [], [], [], [], [], [],
        [], [], [], [], [], [], [], [], [], [], []);

    private static Mock<IMapper> CreateMapperMock()
    {
        var mapper = new Mock<IMapper>();

        mapper.Setup(m => m.Map<PlayerDto>(It.IsAny<object>()))
            .Returns((object? src) => src switch
            {
                IPlayerLogic logic => logic.Player,
                Player player => CreateDto(player.Id, player.Username),
                _ => null!
            });

        mapper.Setup(m => m.Map<List<PlayerDto>>(It.IsAny<object>()))
            .Returns((object src) => ((IEnumerable<Player>)src)
                .Select(p => CreateDto(p.Id, p.Username)).ToList());

        mapper.Setup(m => m.Map<List<PlayerRelationshipDto>>(It.IsAny<object>()))
            .Returns((object src) => ((IEnumerable<PlayerRelationship>)src)
                .Select(r => new PlayerRelationshipDto
                {
                    Id = r.Id,
                    OriginPlayerId = r.OriginPlayerId,
                    TargetPlayerId = r.TargetPlayerId,
                    TypeId = r.TypeId
                }).ToList());

        return mapper;
    }

    private static Mock<IPlayerLogic> CreateLogic(long id, string username)
    {
        var logic = new Mock<IPlayerLogic>();
        logic.SetupGet(x => x.Player).Returns(CreateDto(id, username));
        logic.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        return logic;
    }

    private sealed class StubPacketWriter : AbstractPacketWriter;

    [Test]
    public async Task GetAcceptedFriendshipCountAsync_OfflinePlayer_CountsFromTheDatabase()
    {
        using var factory = new SqliteTestDbFactory();

        using (var db = factory.CreateDbContext())
        {
            for (var id = 1; id <= 4; id++)
            {
                db.Players.Add(new global::Ada.Db.Models.Players.Player
                {
                    Id = id, Username = $"p{id}", Email = "", Password = ""
                });
            }

            db.SaveChanges();

            db.Set<global::Ada.Db.Models.Players.PlayerFriendship>().AddRange(
                new() { OriginPlayerId = 1, TargetPlayerId = 2, Status = PlayerFriendshipStatus.Accepted },
                new() { OriginPlayerId = 3, TargetPlayerId = 1, Status = PlayerFriendshipStatus.Accepted },
                new() { OriginPlayerId = 4, TargetPlayerId = 1, Status = PlayerFriendshipStatus.Pending });

            db.SaveChanges();
        }

        var repository = new PlayerRepository(factory, NullLogger<PlayerRepository>.Instance, CreateMapperMock().Object);

        Assert.That(await repository.GetAcceptedFriendshipCountAsync(1), Is.EqualTo(2));
    }

    [Test]
    public void GetPlayerLogicById_Known_ReturnsLogic()
    {
        using var factory = new SqliteTestDbFactory();
        var repository = new PlayerRepository(factory, NullLogger<PlayerRepository>.Instance, CreateMapperMock().Object);
        var logic = CreateLogic(1, "alice").Object;

        Assert.Multiple(() =>
        {
            Assert.That(repository.TryAddPlayer(logic), Is.True);
            Assert.That(repository.GetPlayerLogicById(1), Is.SameAs(logic));
            Assert.That(repository.GetPlayerLogicById(2), Is.Null);
        });
    }

    [Test]
    public void GetPlayerLogicByUsername_Known_ReturnsLogic()
    {
        using var factory = new SqliteTestDbFactory();
        var repository = new PlayerRepository(factory, NullLogger<PlayerRepository>.Instance, CreateMapperMock().Object);
        var logic = CreateLogic(1, "alice").Object;
        repository.TryAddPlayer(logic);

        Assert.Multiple(() =>
        {
            Assert.That(repository.GetPlayerLogicByUsername("alice"), Is.SameAs(logic));
            Assert.That(repository.GetPlayerLogicByUsername("bob"), Is.Null);
        });
    }

    [Test]
    public void TryAddPlayer_DuplicateId_ReturnsFalse()
    {
        using var factory = new SqliteTestDbFactory();
        var repository = new PlayerRepository(factory, NullLogger<PlayerRepository>.Instance, CreateMapperMock().Object);
        repository.TryAddPlayer(CreateLogic(1, "alice").Object);

        Assert.Multiple(() =>
        {
            Assert.That(repository.TryAddPlayer(CreateLogic(1, "clone").Object), Is.False);
            Assert.That(repository.Count(), Is.EqualTo(1));
            Assert.That(repository.GetAll(), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task TryRemovePlayerAsync_Known_DisposesAndRemoves()
    {
        using var factory = new SqliteTestDbFactory();
        var repository = new PlayerRepository(factory, NullLogger<PlayerRepository>.Instance, CreateMapperMock().Object);
        var logic = CreateLogic(1, "alice");
        repository.TryAddPlayer(logic.Object);

        var result = await repository.TryRemovePlayerAsync(1);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(repository.Count(), Is.Zero);
        });
        logic.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Test]
    public async Task TryRemovePlayerAsync_Unknown_ReturnsFalse()
    {
        using var factory = new SqliteTestDbFactory();
        var repository = new PlayerRepository(factory, NullLogger<PlayerRepository>.Instance, CreateMapperMock().Object);

        Assert.That(await repository.TryRemovePlayerAsync(404), Is.False);
    }

    [Test]
    public async Task GetPlayerByIdAsync_Online_ReturnsCachedPlayer()
    {
        using var factory = new SqliteTestDbFactory();
        var repository = new PlayerRepository(factory, NullLogger<PlayerRepository>.Instance, CreateMapperMock().Object);
        var logic = CreateLogic(1, "alice").Object;
        repository.TryAddPlayer(logic);

        var player = await repository.GetPlayerByIdAsync(1);

        Assert.That(player, Is.SameAs(logic.Player));
    }

    [Test]
    public async Task GetPlayerByIdAsync_Offline_LoadsFromDatabase()
    {
        using var factory = new SqliteTestDbFactory();
        await using (var context = factory.CreateDbContext())
        {
            context.Players.Add(new Player { Id = 7, Username = "offline", Email = "e", Password = "p" });
            await context.SaveChangesAsync();
        }

        var repository = new PlayerRepository(factory, NullLogger<PlayerRepository>.Instance, CreateMapperMock().Object);

        var player = await repository.GetPlayerByIdAsync(7);

        Assert.That(player, Is.Not.Null);
        Assert.That(player!.Username, Is.EqualTo("offline"));
    }

    [Test]
    public async Task GetPlayerByIdAsync_UnknownId_ReturnsNull()
    {
        using var factory = new SqliteTestDbFactory();
        var repository = new PlayerRepository(factory, NullLogger<PlayerRepository>.Instance, CreateMapperMock().Object);

        Assert.That(await repository.GetPlayerByIdAsync(404), Is.Null);
    }

    [Test]
    public async Task GetPlayerByUsernameAsync_Online_MapsLogic()
    {
        using var factory = new SqliteTestDbFactory();
        var repository = new PlayerRepository(factory, NullLogger<PlayerRepository>.Instance, CreateMapperMock().Object);
        var logic = CreateLogic(1, "alice").Object;
        repository.TryAddPlayer(logic);

        var player = await repository.GetPlayerByUsernameAsync("alice");

        Assert.That(player, Is.SameAs(logic.Player));
    }

    [Test]
    public async Task GetPlayerByUsernameAsync_Offline_LoadsFromDatabase()
    {
        using var factory = new SqliteTestDbFactory();
        await using (var context = factory.CreateDbContext())
        {
            context.Players.Add(new Player { Id = 7, Username = "offline", Email = "e", Password = "p" });
            await context.SaveChangesAsync();
        }

        var repository = new PlayerRepository(factory, NullLogger<PlayerRepository>.Instance, CreateMapperMock().Object);

        var player = await repository.GetPlayerByUsernameAsync("offline");

        Assert.That(player, Is.Not.Null);
        Assert.That(player!.Id, Is.EqualTo(7));
    }

    [Test]
    public async Task GetPlayersForSearchAsync_MatchesQueryAndExcludes()
    {
        using var factory = new SqliteTestDbFactory();
        await using (var context = factory.CreateDbContext())
        {
            context.Players.AddRange(
                new Player { Id = 1, Username = "alpha", Email = "e", Password = "p" },
                new Player { Id = 2, Username = "alphabet", Email = "e", Password = "p" },
                new Player { Id = 3, Username = "beta", Email = "e", Password = "p" });
            await context.SaveChangesAsync();
        }

        var repository = new PlayerRepository(factory, NullLogger<PlayerRepository>.Instance, CreateMapperMock().Object);

        var players = await repository.GetPlayersForSearchAsync("alph", [2]);

        Assert.That(players.Select(p => p.Username), Is.EqualTo(new[] { "alpha" }));
    }

    [Test]
    public async Task GetPlayersForSearchAsync_QueryShorterThanMinimum_SkipsTheDatabase()
    {
        using var factory = new SqliteTestDbFactory();
        await using (var context = factory.CreateDbContext())
        {
            context.Players.Add(new Player { Id = 1, Username = "alpha", Email = "e", Password = "p" });
            await context.SaveChangesAsync();
        }

        var repository = new PlayerRepository(factory, NullLogger<PlayerRepository>.Instance, CreateMapperMock().Object);

        Assert.That(await repository.GetPlayersForSearchAsync("al", []), Is.Empty);
    }

    [Test]
    public async Task GetPlayersForSearchAsync_ManyMatches_IsCapped()
    {
        using var factory = new SqliteTestDbFactory();
        await using (var context = factory.CreateDbContext())
        {
            for (var i = 0; i < SearchLimits.MaxPlayerResults + 25; i++)
            {
                context.Players.Add(new Player
                {
                    Id = i + 1,
                    Username = $"alpha{i:D3}",
                    Email = "e",
                    Password = "p"
                });
            }

            await context.SaveChangesAsync();
        }

        var repository = new PlayerRepository(factory, NullLogger<PlayerRepository>.Instance, CreateMapperMock().Object);

        var players = await repository.GetPlayersForSearchAsync("alpha", []);

        Assert.That(players, Has.Count.EqualTo(SearchLimits.MaxPlayerResults));
    }

    [Test]
    public async Task GetPlayersForSearchAsync_MatchesOnPrefixOnly()
    {
        using var factory = new SqliteTestDbFactory();
        await using (var context = factory.CreateDbContext())
        {
            context.Players.AddRange(
                new Player { Id = 1, Username = "alpha", Email = "e", Password = "p" },
                new Player { Id = 2, Username = "notalpha", Email = "e", Password = "p" });
            await context.SaveChangesAsync();
        }

        var repository = new PlayerRepository(factory, NullLogger<PlayerRepository>.Instance, CreateMapperMock().Object);

        var players = await repository.GetPlayersForSearchAsync("alpha", []);

        Assert.That(players.Select(p => p.Username), Is.EqualTo(new[] { "alpha" }));
    }

    [Test]
    public async Task GetRelationshipsForPlayerAsync_ReturnsBothDirections()
    {
        using var factory = new SqliteTestDbFactory();
        await using (var context = factory.CreateDbContext())
        {
            context.Players.AddRange(
                new Player { Id = 1, Username = "a", Email = "e", Password = "p" },
                new Player { Id = 2, Username = "b", Email = "e", Password = "p" },
                new Player { Id = 3, Username = "c", Email = "e", Password = "p" });
            context.Set<PlayerRelationship>().AddRange(
                new PlayerRelationship { OriginPlayerId = 1, TargetPlayerId = 2, TypeId = 1 },
                new PlayerRelationship { OriginPlayerId = 3, TargetPlayerId = 1, TypeId = 2 },
                new PlayerRelationship { OriginPlayerId = 2, TargetPlayerId = 3, TypeId = 3 });
            await context.SaveChangesAsync();
        }

        var repository = new PlayerRepository(factory, NullLogger<PlayerRepository>.Instance, CreateMapperMock().Object);

        var relationships = await repository.GetRelationshipsForPlayerAsync(1);

        Assert.That(relationships, Has.Count.EqualTo(2));
    }

    [Test]
    public void BroadcastDataAsync_NoPlayers_Completes()
    {
        using var factory = new SqliteTestDbFactory();
        var repository = new PlayerRepository(factory, NullLogger<PlayerRepository>.Instance, CreateMapperMock().Object);

        Assert.DoesNotThrowAsync(() => repository.BroadcastDataAsync(new StubPacketWriter()));
    }

    [Test]
    public async Task GetPlayerUsernameByIdAsync_CachesResult()
    {
        using var factory = new SqliteTestDbFactory();
        await using (var context = factory.CreateDbContext())
        {
            context.Players.Add(new Player { Id = 5, Username = "cached", Email = "e", Password = "p" });
            await context.SaveChangesAsync();
        }

        var repository = new PlayerRepository(factory, NullLogger<PlayerRepository>.Instance, CreateMapperMock().Object);

        Assert.That(await repository.GetPlayerUsernameByIdAsync(5), Is.EqualTo("cached"));

        await using (var context = factory.CreateDbContext())
        {
            context.Players.Remove(context.Players.Single(p => p.Id == 5));
            await context.SaveChangesAsync();
        }

        Assert.That(await repository.GetPlayerUsernameByIdAsync(5), Is.EqualTo("cached"));
    }

    [Test]
    public async Task GetPlayerUsernameByIdAsync_Unknown_ReturnsNull()
    {
        using var factory = new SqliteTestDbFactory();
        var repository = new PlayerRepository(factory, NullLogger<PlayerRepository>.Instance, CreateMapperMock().Object);

        Assert.That(await repository.GetPlayerUsernameByIdAsync(404), Is.Null);
    }

    [Test]
    public async Task GetPlayerUsernamesByIdsAsync_ResolvesKnownIdsAndOmitsUnknownOnes()
    {
        using var factory = new SqliteTestDbFactory();
        await using (var context = factory.CreateDbContext())
        {
            context.Players.Add(new Player { Id = 1, Username = "one", Email = "e1", Password = "p" });
            context.Players.Add(new Player { Id = 2, Username = "two", Email = "e2", Password = "p" });
            await context.SaveChangesAsync();
        }

        var repository = new PlayerRepository(factory, NullLogger<PlayerRepository>.Instance, CreateMapperMock().Object);

        var resolved = await repository.GetPlayerUsernamesByIdsAsync([1, 2, 404, 1]);

        Assert.Multiple(() =>
        {
            Assert.That(resolved, Has.Count.EqualTo(2));
            Assert.That(resolved[1], Is.EqualTo("one"));
            Assert.That(resolved[2], Is.EqualTo("two"));
            Assert.That(resolved.ContainsKey(404), Is.False);
        });
    }

    [Test]
    public async Task GetPlayerUsernamesByIdsAsync_ServesCachedNamesWithoutHittingTheDatabase()
    {
        using var factory = new SqliteTestDbFactory();
        await using (var context = factory.CreateDbContext())
        {
            context.Players.Add(new Player { Id = 7, Username = "seven", Email = "e", Password = "p" });
            await context.SaveChangesAsync();
        }

        var repository = new PlayerRepository(factory, NullLogger<PlayerRepository>.Instance, CreateMapperMock().Object);
        await repository.GetPlayerUsernameByIdAsync(7);

        await using (var context = factory.CreateDbContext())
        {
            context.Players.Remove(context.Players.Single(p => p.Id == 7));
            await context.SaveChangesAsync();
        }

        var resolved = await repository.GetPlayerUsernamesByIdsAsync([7]);

        Assert.That(resolved[7], Is.EqualTo("seven"));
    }
}
