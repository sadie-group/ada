using Ada.Db;
using Ada.Db.Models.Rooms;
using Ada.Game.Rooms.Filter;
using Ada.Tests.Common;

namespace Ada.Tests.Game.Rooms.Filter;

[TestFixture]
public class RoomWordFilterServiceTests
{
    private static async Task SeedAsync(IDbContextFactory<AdaDbContext> factory, int roomId, params string[] words)
    {
        await using var db = await factory.CreateDbContextAsync();

        foreach (var word in words)
        {
            db.RoomWordFilters.Add(new RoomWordFilter
            {
                RoomId = roomId,
                Word = word,
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
    }

    [Test]
    public async Task ContainsFilteredWordAsync_NoFilters_ReturnsFalse()
    {
        var factory = TestDbFactory.CreateDbFactory();
        var service = new RoomWordFilterService(factory);

        Assert.That(await service.ContainsFilteredWordAsync(1, "anything at all"), Is.False);
    }

    [Test]
    public async Task ContainsFilteredWordAsync_FilteredWordPresent_ReturnsTrue()
    {
        var factory = TestDbFactory.CreateDbFactory();
        await SeedAsync(factory, 1, "badger");

        var service = new RoomWordFilterService(factory);

        Assert.That(await service.ContainsFilteredWordAsync(1, "look at that BADGER"), Is.True);
    }

    [Test]
    public async Task ContainsFilteredWordAsync_DifferentRoom_IsNotAffected()
    {
        var factory = TestDbFactory.CreateDbFactory();
        await SeedAsync(factory, 1, "badger");

        var service = new RoomWordFilterService(factory);

        Assert.Multiple(async () =>
        {
            Assert.That(await service.ContainsFilteredWordAsync(1, "badger"), Is.True);
            Assert.That(await service.ContainsFilteredWordAsync(2, "badger"), Is.False);
        });
    }

    [Test]
    public async Task ContainsFilteredWordAsync_CachesUntilInvalidated()
    {
        var factory = TestDbFactory.CreateDbFactory();
        var service = new RoomWordFilterService(factory);

        Assert.That(await service.ContainsFilteredWordAsync(1, "badger"), Is.False);

        await SeedAsync(factory, 1, "badger");

        Assert.That(await service.ContainsFilteredWordAsync(1, "badger"), Is.False,
            "the empty result should still be cached");

        service.Invalidate(1);

        Assert.That(await service.ContainsFilteredWordAsync(1, "badger"), Is.True,
            "invalidating should force a reload");
    }
}
