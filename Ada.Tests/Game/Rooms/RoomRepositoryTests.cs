using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Rooms;
using Ada.Game.Rooms;
using Moq;

namespace Ada.Tests.Game.Rooms;

[TestFixture]
public class RoomRepositoryTests
{
    private static Mock<IRoomLogic> CreateRoom(int id, int userCount = 0)
    {
        var room = new Mock<IRoomLogic>();
        room.Setup(x => x.Room).Returns(new RoomDto { Id = id });
        room.Setup(x => x.UserRepository.Count).Returns(userCount);
        return room;
    }

    [Test]
    public void TryGetRoomById_Present_ReturnsRoom()
    {
        var repository = new RoomRepository();
        var room = CreateRoom(1);
        repository.AddRoom(room.Object);

        Assert.That(repository.TryGetRoomById(1), Is.SameAs(room.Object));
    }

    [Test]
    public void TryGetRoomById_Absent_ReturnsNull()
    {
        var repository = new RoomRepository();

        Assert.That(repository.TryGetRoomById(404), Is.Null);
    }

    [Test]
    public void AddRoom_SameId_Replaces()
    {
        var repository = new RoomRepository();
        var first = CreateRoom(1);
        var second = CreateRoom(1);

        repository.AddRoom(first.Object);
        repository.AddRoom(second.Object);

        Assert.That(repository.Count, Is.EqualTo(1));
        Assert.That(repository.TryGetRoomById(1), Is.SameAs(second.Object));
    }

    [Test]
    public void GetPopularRooms_OrdersByUserCountAndExcludesEmpty()
    {
        var repository = new RoomRepository();
        repository.AddRoom(CreateRoom(1, 2).Object);
        repository.AddRoom(CreateRoom(2, 5).Object);
        repository.AddRoom(CreateRoom(3).Object);

        var popular = repository.GetPopularRooms(10);

        Assert.That(popular.Select(x => x.Id), Is.EqualTo(new[] { 2, 1 }));
    }

    [Test]
    public void GetPopularRooms_Amount_Limits()
    {
        var repository = new RoomRepository();
        repository.AddRoom(CreateRoom(1, 2).Object);
        repository.AddRoom(CreateRoom(2, 5).Object);
        repository.AddRoom(CreateRoom(3, 1).Object);

        Assert.That(repository.GetPopularRooms(1).Single().Id, Is.EqualTo(2));
    }

    [Test]
    public void GetAllRooms_ReturnsAll()
    {
        var repository = new RoomRepository();
        repository.AddRoom(CreateRoom(1).Object);
        repository.AddRoom(CreateRoom(2).Object);

        Assert.That(repository.GetAllRooms().Count(), Is.EqualTo(2));
    }

    [Test]
    public void TryRemove_Present_RemovesAndReturnsRoom()
    {
        var repository = new RoomRepository();
        var room = CreateRoom(1);
        repository.AddRoom(room.Object);

        Assert.That(repository.TryRemove(1, out var removed), Is.True);
        Assert.That(removed, Is.SameAs(room.Object));
        Assert.That(repository.Count, Is.Zero);
    }

    [Test]
    public void TryRemove_Absent_ReturnsFalse()
    {
        var repository = new RoomRepository();

        Assert.That(repository.TryRemove(404, out var removed), Is.False);
        Assert.That(removed, Is.Null);
    }

    [Test]
    public async Task DisposeAsync_DisposesAllRoomsAndClears()
    {
        var repository = new RoomRepository();
        var first = CreateRoom(1);
        var second = CreateRoom(2);
        repository.AddRoom(first.Object);
        repository.AddRoom(second.Object);

        await repository.DisposeAsync();

        first.Verify(x => x.DisposeAsync(), Times.Once);
        second.Verify(x => x.DisposeAsync(), Times.Once);
        Assert.That(repository.Count, Is.Zero);
    }
}
