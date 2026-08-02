using Ada.Db.Models;
using Ada.Db.Models.Players;
using Ada.Db.Models.Rooms;
using Ada.Db.Models.Rooms.Chat;
using Ada.Game.Moderation;
using Ada.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace Ada.Tests.Game.Moderation;

[TestFixture]
public class ModToolRepositoryTests
{
    private static Player NewPlayer(long id, string username) => new()
    {
        Id = id,
        Username = username,
        Email = $"{username}@test.local",
        Password = "secret",
        CreatedAt = DateTimeOffset.FromUnixTimeSeconds(1000 * id)
    };

    private static Room NewRoom(int id, string name) => new()
    {
        Id = id,
        Name = name,
        Description = ""
    };

    [Test]
    public async Task GetUserInfoAsync_UnknownUser_ReturnsNull()
    {
        var factory = TestDbFactory.CreateDbFactory();
        var repository = new ModToolRepository(factory);

        Assert.That(await repository.GetUserInfoAsync(42), Is.Null);
    }

    [Test]
    public async Task GetUserInfoAsync_UserWithAvatarRolesAndBans_MapsAllFields()
    {
        var factory = TestDbFactory.CreateDbFactory();

        await using (var context = await factory.CreateDbContextAsync())
        {
            var target = NewPlayer(1, "target");
            target.AvatarData = new PlayerAvatarData { PlayerId = 1, FigureCode = "hd-180-1" };
            target.Roles.Add(new Role { Id = 1, Name = "Helper" });
            target.Roles.Add(new Role { Id = 5, Name = "Admin" });
            var moderator = NewPlayer(2, "mod");

            context.Players.AddRange(target, moderator);
            context.PlayerBans.Add(new PlayerBan
                { CreatorId = 2, Creator = moderator, PlayerId = 1, Player = target, Reason = "first" });
            context.PlayerBans.Add(new PlayerBan
                { CreatorId = 2, Creator = moderator, PlayerId = 1, Player = target, Reason = "second" });
            await context.SaveChangesAsync();
        }

        var info = await new ModToolRepository(factory).GetUserInfoAsync(1);

        Assert.That(info, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(info!.UserId, Is.EqualTo(1));
            Assert.That(info.Username, Is.EqualTo("target"));
            Assert.That(info.Look, Is.EqualTo("hd-180-1"));
            Assert.That(info.Email, Is.EqualTo("target@test.local"));
            Assert.That(info.CreatedAt, Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(1000)));
            Assert.That(info.RankId, Is.EqualTo(5));
            Assert.That(info.RankName, Is.EqualTo("Admin"));
            Assert.That(info.BanCount, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task GetUserInfoAsync_UserWithoutAvatarRolesOrBans_UsesDefaults()
    {
        var factory = TestDbFactory.CreateDbFactory();

        await using (var context = await factory.CreateDbContextAsync())
        {
            context.Players.Add(NewPlayer(1, "plain"));
            await context.SaveChangesAsync();
        }

        var info = await new ModToolRepository(factory).GetUserInfoAsync(1);

        Assert.That(info, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(info!.Look, Is.EqualTo(""));
            Assert.That(info.RankId, Is.EqualTo(0));
            Assert.That(info.RankName, Is.EqualTo("User"));
            Assert.That(info.BanCount, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task GetRoomVisitsAsync_UnknownUser_ReturnsEmptyUsernameAndNoVisits()
    {
        var factory = TestDbFactory.CreateDbFactory();

        var (username, visits) = await new ModToolRepository(factory).GetRoomVisitsAsync(42, 10);

        Assert.Multiple(() =>
        {
            Assert.That(username, Is.EqualTo(""));
            Assert.That(visits, Is.Empty);
        });
    }

    [Test]
    public async Task GetRoomVisitsAsync_ReturnsMostRecentVisitsWithRoomNames()
    {
        var factory = TestDbFactory.CreateDbFactory();

        await using (var context = await factory.CreateDbContextAsync())
        {
            context.Players.Add(NewPlayer(1, "visitor"));
            context.Rooms.AddRange(NewRoom(10, "lobby"), NewRoom(11, "pool"));
            context.PlayerRoomVisits.AddRange(
                new PlayerRoomVisit { PlayerId = 1, RoomId = 10, CreatedAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc) },
                new PlayerRoomVisit { PlayerId = 1, RoomId = 11, CreatedAt = new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc) },
                new PlayerRoomVisit { PlayerId = 1, RoomId = 10, CreatedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc) },
                new PlayerRoomVisit { PlayerId = 2, RoomId = 10, CreatedAt = new DateTime(2026, 1, 1, 13, 0, 0, DateTimeKind.Utc) });
            await context.SaveChangesAsync();
        }

        var (username, visits) = await new ModToolRepository(factory).GetRoomVisitsAsync(1, 2);

        Assert.Multiple(() =>
        {
            Assert.That(username, Is.EqualTo("visitor"));
            Assert.That(visits, Has.Count.EqualTo(2));
            Assert.That(visits[0].RoomId, Is.EqualTo(10));
            Assert.That(visits[0].RoomName, Is.EqualTo("lobby"));
            Assert.That(visits[0].EnteredAt, Is.EqualTo((DateTimeOffset)new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)));
            Assert.That(visits[1].RoomId, Is.EqualTo(11));
            Assert.That(visits[1].RoomName, Is.EqualTo("pool"));
        });
    }

    [Test]
    public async Task GetUserChatlogAsync_UnknownUser_ReturnsEmptyUsernameAndNoRooms()
    {
        var factory = TestDbFactory.CreateDbFactory();

        var (username, rooms) = await new ModToolRepository(factory).GetUserChatlogAsync(42, 10);

        Assert.Multiple(() =>
        {
            Assert.That(username, Is.EqualTo(""));
            Assert.That(rooms, Is.Empty);
        });
    }

    [Test]
    public async Task GetUserChatlogAsync_GroupsMessagesPerRoomOrderedByTime()
    {
        var factory = TestDbFactory.CreateDbFactory();

        await using (var context = await factory.CreateDbContextAsync())
        {
            var player = NewPlayer(1, "chatter");
            context.Players.Add(player);
            context.Rooms.AddRange(NewRoom(10, "lobby"), NewRoom(11, "pool"));
            context.RoomChatMessages.AddRange(
                new RoomChatMessage { Id = 1, RoomId = 10, PlayerId = 1, Player = player, Message = "hello", CreatedAt = DateTimeOffset.FromUnixTimeSeconds(100) },
                new RoomChatMessage { Id = 2, RoomId = 10, PlayerId = 1, Player = player, Message = null, CreatedAt = DateTimeOffset.FromUnixTimeSeconds(200) },
                new RoomChatMessage { Id = 3, RoomId = 11, PlayerId = 1, Player = player, Message = "bye", CreatedAt = DateTimeOffset.FromUnixTimeSeconds(300) });
            await context.SaveChangesAsync();
        }

        var (username, rooms) = await new ModToolRepository(factory).GetUserChatlogAsync(1, 10);

        Assert.That(username, Is.EqualTo("chatter"));
        Assert.That(rooms, Has.Count.EqualTo(2));

        var lobby = rooms.Single(r => r.RoomId == 10);
        Assert.Multiple(() =>
        {
            Assert.That(lobby.RoomName, Is.EqualTo("lobby"));
            Assert.That(lobby.Lines, Has.Count.EqualTo(2));
            Assert.That(lobby.Lines[0].Message, Is.EqualTo("hello"));
            Assert.That(lobby.Lines[0].Username, Is.EqualTo("chatter"));
            Assert.That(lobby.Lines[0].PlayerId, Is.EqualTo(1));
            Assert.That(lobby.Lines[1].Message, Is.EqualTo(""));
            Assert.That(rooms.Single(r => r.RoomId == 11).Lines.Single().Message, Is.EqualTo("bye"));
        });
    }

    [Test]
    public async Task GetUserChatlogAsync_LimitKeepsMostRecentMessages()
    {
        var factory = TestDbFactory.CreateDbFactory();

        await using (var context = await factory.CreateDbContextAsync())
        {
            var player = NewPlayer(1, "chatter");
            context.Players.Add(player);
            context.Rooms.Add(NewRoom(10, "lobby"));
            context.RoomChatMessages.AddRange(
                new RoomChatMessage { Id = 1, RoomId = 10, PlayerId = 1, Player = player, Message = "oldest", CreatedAt = DateTimeOffset.FromUnixTimeSeconds(100) },
                new RoomChatMessage { Id = 2, RoomId = 10, PlayerId = 1, Player = player, Message = "middle", CreatedAt = DateTimeOffset.FromUnixTimeSeconds(200) },
                new RoomChatMessage { Id = 3, RoomId = 10, PlayerId = 1, Player = player, Message = "newest", CreatedAt = DateTimeOffset.FromUnixTimeSeconds(300) });
            await context.SaveChangesAsync();
        }

        var (_, rooms) = await new ModToolRepository(factory).GetUserChatlogAsync(1, 2);

        var lines = rooms.Single().Lines;
        Assert.Multiple(() =>
        {
            Assert.That(lines, Has.Count.EqualTo(2));
            Assert.That(lines[0].Message, Is.EqualTo("middle"));
            Assert.That(lines[1].Message, Is.EqualTo("newest"));
        });
    }

    [Test]
    public async Task CreateBanAsync_UnknownModerator_ReturnsFalse()
    {
        var factory = TestDbFactory.CreateDbFactory();

        await using (var context = await factory.CreateDbContextAsync())
        {
            context.Players.Add(NewPlayer(1, "target"));
            await context.SaveChangesAsync();
        }

        Assert.That(await new ModToolRepository(factory).CreateBanAsync(99, 1, "reason", null), Is.False);
    }

    [Test]
    public async Task CreateBanAsync_UnknownTarget_ReturnsFalse()
    {
        var factory = TestDbFactory.CreateDbFactory();

        await using (var context = await factory.CreateDbContextAsync())
        {
            context.Players.Add(NewPlayer(2, "mod"));
            await context.SaveChangesAsync();
        }

        Assert.That(await new ModToolRepository(factory).CreateBanAsync(2, 99, "reason", null), Is.False);
    }

    [Test]
    public async Task CreateBanAsync_ValidPlayers_PersistsBan()
    {
        var factory = TestDbFactory.CreateDbFactory();

        await using (var context = await factory.CreateDbContextAsync())
        {
            context.Players.AddRange(NewPlayer(1, "target"), NewPlayer(2, "mod"));
            await context.SaveChangesAsync();
        }

        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);
        var created = await new ModToolRepository(factory).CreateBanAsync(2, 1, "spam", expiresAt);

        Assert.That(created, Is.True);

        await using (var context = await factory.CreateDbContextAsync())
        {
            var ban = await context.PlayerBans.SingleAsync();
            Assert.Multiple(() =>
            {
                Assert.That(ban.CreatorId, Is.EqualTo(2));
                Assert.That(ban.PlayerId, Is.EqualTo(1));
                Assert.That(ban.Reason, Is.EqualTo("spam"));
                Assert.That(ban.ExpiresAt, Is.EqualTo(expiresAt));
            });
        }
    }
}
