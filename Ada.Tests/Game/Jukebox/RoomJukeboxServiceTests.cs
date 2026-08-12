using Ada.Db;
using Ada.Db.Models;
using Ada.Db.Models.Players;
using Ada.Db.Models.Furniture;
using Ada.Db.Models.Players.Furniture;
using Ada.Game.Jukebox;
using Ada.Tests.Common;

namespace Ada.Tests.Game.Jukebox;

[TestFixture]
public class RoomJukeboxServiceTests
{
    private const long _playerId = 1;

    private static async Task<int> SeedDiscAsync(
        IDbContextFactory<AdaDbContext> factory,
        int soundTrackId,
        long playerId = _playerId,
        bool withTrack = true)
    {
        await using var db = await factory.CreateDbContextAsync();

        if (withTrack && !db.SoundTracks.Any(x => x.Id == soundTrackId))
        {
            db.SoundTracks.Add(new SoundTrack
            {
                Id = soundTrackId,
                Name = "track" + soundTrackId,
                Author = "author",
                Code = "code" + soundTrackId,
                Data = "data",
                Length = 60
            });
        }

        var owner = db.Players.FirstOrDefault(x => x.Id == playerId);

        if (owner == null)
        {
            owner = new Player
            {
                Id = playerId,
                Username = "player" + playerId,
                Email = playerId + "@test.com",
                Password = "secret"
            };

            db.Players.Add(owner);
        }

        var furnitureItem = db.FurnitureItems.FirstOrDefault(x => x.Id == 1);

        if (furnitureItem == null)
        {
            furnitureItem = new FurnitureItem
            {
                Id = 1,
                Name = "song_disk",
                AssetName = "song_disk",
                InteractionType = "song_disk"
            };

            db.FurnitureItems.Add(furnitureItem);
        }

        var disc = new PlayerFurnitureItem
        {
            PlayerId = playerId,
            Player = owner,
            FurnitureItemId = furnitureItem.Id,
            FurnitureItem = furnitureItem,
            LimitedData = string.Empty,
            MetaData = soundTrackId.ToString()
        };

        db.PlayerFurnitureItems.Add(disc);

        await db.SaveChangesAsync();

        return disc.Id;
    }

    [Test]
    public async Task TryAddAsync_ValidDisc_IsAddedAtTheFirstFreeSlot()
    {
        var factory = TestDbFactory.CreateDbFactory();
        var service = new RoomJukeboxService(factory);
        var discId = await SeedDiscAsync(factory, 10);

        Assert.That(await service.TryAddAsync(1, _playerId, discId), Is.True);

        var playlist = await service.GetPlaylistAsync(1);

        Assert.Multiple(() =>
        {
            Assert.That(playlist, Has.Count.EqualTo(1));
            Assert.That(playlist[0].SoundTrackId, Is.EqualTo(10));
            Assert.That(playlist[0].OrderIndex, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task TryAddAsync_DiscOwnedBySomebodyElse_IsRejected()
    {
        var factory = TestDbFactory.CreateDbFactory();
        var service = new RoomJukeboxService(factory);
        var discId = await SeedDiscAsync(factory, 10, playerId: 99);

        Assert.That(await service.TryAddAsync(1, _playerId, discId), Is.False);
        Assert.That(await service.GetPlaylistAsync(1), Is.Empty);
    }

    [Test]
    public async Task TryAddAsync_UnknownSoundTrack_IsRejected()
    {
        var factory = TestDbFactory.CreateDbFactory();
        var service = new RoomJukeboxService(factory);
        var discId = await SeedDiscAsync(factory, 77, withTrack: false);

        Assert.That(await service.TryAddAsync(1, _playerId, discId), Is.False);
    }

    [Test]
    public async Task TryAddAsync_SameDiscTwice_IsRejected()
    {
        var factory = TestDbFactory.CreateDbFactory();
        var service = new RoomJukeboxService(factory);
        var discId = await SeedDiscAsync(factory, 10);

        Assert.That(await service.TryAddAsync(1, _playerId, discId), Is.True);
        Assert.That(await service.TryAddAsync(1, _playerId, discId), Is.False);
    }

    [Test]
    public async Task TryAddAsync_BeyondCapacity_IsRejected()
    {
        var factory = TestDbFactory.CreateDbFactory();
        var service = new RoomJukeboxService(factory);

        for (var i = 0; i < service.MaxTracksPerRoom; i++)
        {
            var id = await SeedDiscAsync(factory, 100 + i);
            Assert.That(await service.TryAddAsync(1, _playerId, id), Is.True);
        }

        var overflow = await SeedDiscAsync(factory, 999);

        Assert.That(await service.TryAddAsync(1, _playerId, overflow), Is.False);
        Assert.That(await service.GetPlaylistAsync(1), Has.Count.EqualTo(service.MaxTracksPerRoom));
    }

    [Test]
    public async Task TryRemoveAsync_FreesTheSlotForReuse()
    {
        var factory = TestDbFactory.CreateDbFactory();
        var service = new RoomJukeboxService(factory);

        var first = await SeedDiscAsync(factory, 10);
        var second = await SeedDiscAsync(factory, 11);

        await service.TryAddAsync(1, _playerId, first);
        await service.TryAddAsync(1, _playerId, second);

        Assert.That(await service.TryRemoveAsync(1, 0), Is.EqualTo(first));

        var third = await SeedDiscAsync(factory, 12);

        Assert.That(await service.TryAddAsync(1, _playerId, third), Is.True);

        var playlist = await service.GetPlaylistAsync(1);

        Assert.That(playlist.Select(x => x.OrderIndex), Is.EquivalentTo(new[] { 0, 1 }));
    }

    [Test]
    public async Task TryRemoveAsync_EmptySlot_ReturnsNull()
    {
        var factory = TestDbFactory.CreateDbFactory();
        var service = new RoomJukeboxService(factory);

        Assert.That(await service.TryRemoveAsync(1, 3), Is.Null);
    }

    [Test]
    public async Task GetPlaylistAsync_IsScopedToTheRoom()
    {
        var factory = TestDbFactory.CreateDbFactory();
        var service = new RoomJukeboxService(factory);

        var disc = await SeedDiscAsync(factory, 10);
        await service.TryAddAsync(1, _playerId, disc);

        Assert.Multiple(async () =>
        {
            Assert.That(await service.GetPlaylistAsync(1), Has.Count.EqualTo(1));
            Assert.That(await service.GetPlaylistAsync(2), Is.Empty);
        });
    }
}
