using Ada.Db.Models;
using Ada.Game.Jukebox;
using Ada.Tests.Common;

namespace Ada.Tests.Game.Jukebox;

[TestFixture]
public class SoundTrackRepositoryTests
{
    private static async Task<SoundTrackRepository> CreateSeededAsync()
    {
        var factory = TestDbFactory.CreateDbFactory();

        await using (var db = await factory.CreateDbContextAsync())
        {
            db.SoundTracks.Add(new SoundTrack
                { Id = 1, Name = "song-one", Author = "alice", Code = "c1", Data = "d1", Length = 60 });
            db.SoundTracks.Add(new SoundTrack
                { Id = 2, Name = "song-two", Author = "bob", Code = "c2", Data = "d2", Length = 90 });
            db.SoundTracks.Add(new SoundTrack
                { Id = 3, Name = "song-three", Author = "carol", Code = "c3", Data = "d3", Length = 120 });
            await db.SaveChangesAsync();
        }

        return new SoundTrackRepository(factory);
    }

    [Test]
    public async Task GetByIdsAsync_ReturnsMatchingTracks()
    {
        var repository = await CreateSeededAsync();

        var tracks = await repository.GetByIdsAsync([1, 3]);

        Assert.That(tracks, Has.Count.EqualTo(2));
        var first = tracks.Single(x => x.Id == 1);
        Assert.Multiple(() =>
        {
            Assert.That(first.Name, Is.EqualTo("song-one"));
            Assert.That(first.Author, Is.EqualTo("alice"));
            Assert.That(first.Code, Is.EqualTo("c1"));
            Assert.That(first.Data, Is.EqualTo("d1"));
            Assert.That(first.Length, Is.EqualTo(60));
        });
    }

    [Test]
    public async Task GetByIdsAsync_UnknownIds_ReturnsEmpty()
    {
        var repository = await CreateSeededAsync();

        var tracks = await repository.GetByIdsAsync([404, 405]);

        Assert.That(tracks, Is.Empty);
    }

    [Test]
    public async Task GetByNameAsync_Found_MapsFields()
    {
        var repository = await CreateSeededAsync();

        var track = await repository.GetByNameAsync("song-two");

        Assert.That(track, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(track.Id, Is.EqualTo(2));
            Assert.That(track.Author, Is.EqualTo("bob"));
            Assert.That(track.Code, Is.EqualTo("c2"));
            Assert.That(track.Data, Is.EqualTo("d2"));
            Assert.That(track.Length, Is.EqualTo(90));
        });
    }

    [Test]
    public async Task GetByNameAsync_Unknown_ReturnsNull()
    {
        var repository = await CreateSeededAsync();

        Assert.That(await repository.GetByNameAsync("missing"), Is.Null);
    }
}
