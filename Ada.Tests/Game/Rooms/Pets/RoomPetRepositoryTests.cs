using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Rooms.Pets;
using Ada.Game.Rooms.Pets;
using Moq;

namespace Ada.Tests.Game.Rooms.Pets;

[TestFixture]
public class RoomPetRepositoryTests
{
    private static Mock<IRoomPet> CreatePet(int id)
    {
        var pet = new Mock<IRoomPet>();
        pet.Setup(x => x.Pet).Returns(new PlayerPetDto { Id = id });
        pet.Setup(x => x.RunPeriodicCheckAsync()).Returns(Task.CompletedTask);
        return pet;
    }

    [Test]
    public void TryAdd_NewPet_Adds()
    {
        var repository = new RoomPetRepository();
        var pet = CreatePet(1);
        Assert.Multiple(() =>
        {
            Assert.That(repository.TryAdd(pet.Object), Is.True);
            Assert.That(repository.Count, Is.EqualTo(1));
        });
        Assert.That(repository.GetAll().Single(), Is.SameAs(pet.Object));
    }

    [Test]
    public void TryAdd_DuplicateId_Rejects()
    {
        var repository = new RoomPetRepository();
        repository.TryAdd(CreatePet(1).Object);
        Assert.Multiple(() =>
        {
            Assert.That(repository.TryAdd(CreatePet(1).Object), Is.False);
            Assert.That(repository.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void TryGetById_Present_ReturnsPet()
    {
        var repository = new RoomPetRepository();
        var pet = CreatePet(7);
        repository.TryAdd(pet.Object);
        Assert.Multiple(() =>
        {
            Assert.That(repository.TryGetById(7, out var found), Is.True);
            Assert.That(found, Is.SameAs(pet.Object));
        });
    }

    [Test]
    public void TryGetById_Absent_ReturnsFalse()
    {
        var repository = new RoomPetRepository();
        Assert.Multiple(() =>
        {
            Assert.That(repository.TryGetById(404, out var found), Is.False);
            Assert.That(found, Is.Null);
        });
    }

    [Test]
    public void TryRemove_Present_Removes()
    {
        var repository = new RoomPetRepository();
        var pet = CreatePet(1);
        repository.TryAdd(pet.Object);
        Assert.Multiple(() =>
        {
            Assert.That(repository.TryRemove(1, out var removed), Is.True);
            Assert.That(removed, Is.SameAs(pet.Object));
            Assert.That(repository.Count, Is.Zero);
        });
    }

    [Test]
    public void TryRemove_Absent_ReturnsFalse()
    {
        var repository = new RoomPetRepository();

        Assert.That(repository.TryRemove(404, out _), Is.False);
    }

    [Test]
    public void Loaded_Settable()
    {
        var repository = new RoomPetRepository();

        Assert.That(repository.Loaded, Is.False);
        repository.Loaded = true;
        Assert.That(repository.Loaded, Is.True);
    }

    [Test]
    public void TryStartBreeding_NewNest_Starts()
    {
        var repository = new RoomPetRepository();
        Assert.Multiple(() =>
        {
            Assert.That(repository.TryStartBreeding(1, 10, 11), Is.True);
            Assert.That(repository.TryGetBreeding(1, out var pets), Is.True);
            Assert.That(pets, Is.EqualTo((10, 11)));
        });
    }

    [Test]
    public void TryStartBreeding_OccupiedNest_Rejects()
    {
        var repository = new RoomPetRepository();
        repository.TryStartBreeding(1, 10, 11);

        Assert.That(repository.TryStartBreeding(1, 12, 13), Is.False);
        repository.TryGetBreeding(1, out var pets);
        Assert.That(pets, Is.EqualTo((10, 11)));
    }

    [Test]
    public void TryGetBreeding_UnknownNest_ReturnsFalse()
    {
        var repository = new RoomPetRepository();

        Assert.That(repository.TryGetBreeding(404, out _), Is.False);
    }

    [Test]
    public void StopBreeding_RemovesNest()
    {
        var repository = new RoomPetRepository();
        repository.TryStartBreeding(1, 10, 11);

        repository.StopBreeding(1);

        Assert.That(repository.TryGetBreeding(1, out _), Is.False);
    }

    [Test]
    public async Task RunPeriodicCheckAsync_ChecksEveryPet()
    {
        var repository = new RoomPetRepository();
        var first = CreatePet(1);
        var second = CreatePet(2);
        repository.TryAdd(first.Object);
        repository.TryAdd(second.Object);

        await repository.RunPeriodicCheckAsync();

        first.Verify(x => x.RunPeriodicCheckAsync(), Times.Once);
        second.Verify(x => x.RunPeriodicCheckAsync(), Times.Once);
    }

    [Test]
    public void RunPeriodicCheckAsync_PetThrows_Swallows()
    {
        var repository = new RoomPetRepository();
        var pet = CreatePet(1);
        pet.Setup(x => x.RunPeriodicCheckAsync()).ThrowsAsync(new InvalidOperationException("boom"));
        repository.TryAdd(pet.Object);

        Assert.DoesNotThrowAsync(() => repository.RunPeriodicCheckAsync());
    }

    [Test]
    public void DisposeAsync_Completes()
    {
        var repository = new RoomPetRepository();

        Assert.DoesNotThrowAsync(async () => await repository.DisposeAsync());
    }
}
