using Ada.API.DTOs.Players;
using Ada.Db.Models.Players;
using Ada.Game.Pets;
using Ada.Tests.Common;

namespace Ada.Tests.Game.Pets;

[TestFixture]
public class PlayerPetPersistenceTests
{
    private static async Task<int> SeedPetAsync(SqliteTestDbFactory factory)
    {
        await using var db = factory.CreateDbContext();

        if (!db.Players.Any(x => x.Id == 1))
        {
            db.Players.Add(new Player { Id = 1, Username = "alice", Email = "a@test.com", Password = "secret" });
            await db.SaveChangesAsync();
        }

        var pet = new PlayerPet
        {
            PlayerId = 1,
            RoomId = null,
            Name = "rex",
            Type = 0,
            Race = 1,
            Color = "ffffff",
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.PlayerPets.Add(pet);
        await db.SaveChangesAsync();

        return pet.Id;
    }

    private static PlayerPetDto Dto(int id) => new() { Id = id, PlayerId = 1, Name = "rex" };

    [Test]
    public async Task SaveRideSettingsAsync_PersistsAnyoneCanRide()
    {
        using var factory = new SqliteTestDbFactory();
        var id = await SeedPetAsync(factory);

        var dto = Dto(id);
        dto.AnyoneCanRide = true;

        await new PlayerPetPersistence(factory).SaveRideSettingsAsync(dto);

        await using var db = factory.CreateDbContext();
        Assert.That(db.PlayerPets.Single(x => x.Id == id).AnyoneCanRide, Is.True);
    }

    [Test]
    public async Task SaveSaddleAsync_PersistsHasSaddle()
    {
        using var factory = new SqliteTestDbFactory();
        var id = await SeedPetAsync(factory);

        var dto = Dto(id);
        dto.HasSaddle = true;

        await new PlayerPetPersistence(factory).SaveSaddleAsync(dto);

        await using var db = factory.CreateDbContext();
        Assert.That(db.PlayerPets.Single(x => x.Id == id).HasSaddle, Is.True);
    }

    [Test]
    public async Task SaveBreedingAsync_PersistsPubliclyBreedable()
    {
        using var factory = new SqliteTestDbFactory();
        var id = await SeedPetAsync(factory);

        var dto = Dto(id);
        dto.PubliclyBreedable = true;

        await new PlayerPetPersistence(factory).SaveBreedingAsync(dto);

        await using var db = factory.CreateDbContext();
        Assert.That(db.PlayerPets.Single(x => x.Id == id).PubliclyBreedable, Is.True);
    }

    [Test]
    public async Task SaveScratchAsync_PersistsAllFourCounters()
    {
        using var factory = new SqliteTestDbFactory();
        var id = await SeedPetAsync(factory);

        var dto = Dto(id);
        dto.Respect = 3;
        dto.Happiness = 40;
        dto.Experience = 250;
        dto.Level = 5;

        await new PlayerPetPersistence(factory).SaveScratchAsync(dto);

        await using var db = factory.CreateDbContext();
        var row = db.PlayerPets.Single(x => x.Id == id);

        Assert.Multiple(() =>
        {
            Assert.That(row.Respect, Is.EqualTo(3));
            Assert.That(row.Happiness, Is.EqualTo(40));
            Assert.That(row.Experience, Is.EqualTo(250));
            Assert.That(row.Level, Is.EqualTo(5));
        });
    }

    [Test]
    public async Task DeleteAsync_RemovesTheRow()
    {
        using var factory = new SqliteTestDbFactory();
        var id = await SeedPetAsync(factory);

        await new PlayerPetPersistence(factory).DeleteAsync(id);

        await using var db = factory.CreateDbContext();
        Assert.That(db.PlayerPets.Any(x => x.Id == id), Is.False);
    }
}
