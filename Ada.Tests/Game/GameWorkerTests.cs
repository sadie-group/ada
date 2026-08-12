using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Game;
using Moq;

namespace Ada.Tests.Game;

[TestFixture]
public class GameWorkerTests
{
    private static IRoomLogic Room(int id, int userCount, DateTime? noUsersSince = null)
    {
        var users = new Mock<IRoomUserRepository>();
        users.SetupGet(u => u.Count).Returns(userCount);
        users.SetupProperty(u => u.NoUsersSince, noUsersSince);

        var room = new Mock<IRoomLogic>();
        room.SetupGet(r => r.UserRepository).Returns(users.Object);
        room.SetupGet(r => r.Room).Returns(new RoomDto { Id = id });

        return room.Object;
    }

    private static GameWorker Worker(params IRoomLogic[] rooms)
    {
        var repository = new Mock<IRoomRepository>();
        repository.Setup(r => r.GetAllRooms()).Returns(rooms);

        return new GameWorker(
            repository.Object,
            Mock.Of<IRoomWiredService>(),
            NullLogger<GameWorker>.Instance);
    }

    [Test]
    public void CollectActiveRooms_SkipsEmptyRooms()
    {
        var busy = Room(1, userCount: 2);
        var empty = Room(2, userCount: 0);

        var active = Worker(busy, empty).CollectActiveRooms();

        Assert.That(active, Is.EqualTo(new[] { busy }),
            "an empty room must not cost a parallel slot on every pass");
    }

    [Test]
    public void CollectActiveRooms_StampsNoUsersSinceOnEmptyRooms()
    {
        var empty = Room(1, userCount: 0);

        Worker(empty).CollectActiveRooms();

        Assert.That(empty.UserRepository.NoUsersSince, Is.Not.Null,
            "idle rooms still need their unload clock started");
    }

    [Test]
    public void CollectActiveRooms_DoesNotResetAnExistingNoUsersSince()
    {
        var stampedAt = DateTime.UtcNow.AddMinutes(-30);
        var empty = Room(1, userCount: 0, noUsersSince: stampedAt);

        Worker(empty).CollectActiveRooms();

        Assert.That(empty.UserRepository.NoUsersSince, Is.EqualTo(stampedAt),
            "restamping would keep an idle room loaded forever");
    }

    [Test]
    public void CollectActiveRooms_ClearsTheListBetweenPasses()
    {
        var busy = Room(1, userCount: 1);
        var worker = Worker(busy);

        worker.CollectActiveRooms();
        var second = worker.CollectActiveRooms();

        Assert.That(second, Has.Count.EqualTo(1), "the reused buffer must not accumulate across passes");
    }

    [Test]
    public void CollectActiveRooms_NoRooms_ReturnsEmpty()
    {
        Assert.That(Worker().CollectActiveRooms(), Is.Empty);
    }
}
