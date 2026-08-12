using Ada.Db;
using Ada.Game.Moderation;
using Ada.Tests.Common;

namespace Ada.Tests.Game.Moderation;

[TestFixture]
public class ModerationAuditServiceTests
{
    private static ModerationAuditService CreateService(IDbContextFactory<AdaDbContext> factory)
        => new(factory, NullLogger<ModerationAuditService>.Instance);

    [Test]
    public async Task RecordAsync_WritesTheEntry()
    {
        var factory = TestDbFactory.CreateDbFactory();
        var service = CreateService(factory);

        await service.RecordAsync(7, "moderator", "ban", targetPlayerId: 42, reason: "spam");

        await using var db = await factory.CreateDbContextAsync();
        var entry = db.ModerationAuditEntries.Single();

        Assert.Multiple(() =>
        {
            Assert.That(entry.ModeratorId, Is.EqualTo(7));
            Assert.That(entry.ModeratorUsername, Is.EqualTo("moderator"));
            Assert.That(entry.Action, Is.EqualTo("ban"));
            Assert.That(entry.TargetPlayerId, Is.EqualTo(42));
            Assert.That(entry.Reason, Is.EqualTo("spam"));
        });
    }

    [Test]
    public async Task RecordAsync_TruncatesAnOverlongReason()
    {
        var factory = TestDbFactory.CreateDbFactory();
        var service = CreateService(factory);

        await service.RecordAsync(1, "mod", "mute", reason: new string('x', 900));

        await using var db = await factory.CreateDbContextAsync();

        Assert.That(db.ModerationAuditEntries.Single().Reason, Has.Length.EqualTo(512));
    }

    [Test]
    public async Task RecordAsync_RoomAction_StoresTheRoomAndNoPlayer()
    {
        var factory = TestDbFactory.CreateDbFactory();
        var service = CreateService(factory);

        await service.RecordAsync(1, "mod", "room-settings", targetRoomId: 55);

        await using var db = await factory.CreateDbContextAsync();
        var entry = db.ModerationAuditEntries.Single();

        Assert.Multiple(() =>
        {
            Assert.That(entry.TargetRoomId, Is.EqualTo(55));
            Assert.That(entry.TargetPlayerId, Is.Null);
        });
    }

    [Test]
    public async Task RecordAsync_ManyActions_AreAllRetained()
    {
        var factory = TestDbFactory.CreateDbFactory();
        var service = CreateService(factory);

        for (var i = 0; i < 5; i++)
        {
            await service.RecordAsync(1, "mod", "kick", targetPlayerId: i);
        }

        await using var db = await factory.CreateDbContextAsync();

        Assert.That(db.ModerationAuditEntries.Count(), Is.EqualTo(5));
    }
}
