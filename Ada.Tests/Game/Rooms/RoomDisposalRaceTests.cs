using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Bots;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Pathfinding;
using Ada.API.Interfaces.Game.Rooms.Pets;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Game.Rooms;
using Ada.Game.Rooms.Locking;
using Moq;

namespace Ada.Tests.Game.Rooms;

[TestFixture]
public class RoomDisposalRaceTests
{
    private sealed class CountingUserRepository : IRoomUserRepository
    {
        private int _count;

        public int Count => _count;
        public DateTime? NoUsersSince { get; set; }
        public bool Disposed { get; private set; }

        public void Join() => _count++;

        public ICollection<IRoomUser> GetAll() => [];
        public bool TryAdd(IRoomUser user) => true;
        public bool TryGetById(long id, out IRoomUser? user) { user = null; return false; }
        public bool TryGetByUsername(string username, out IRoomUser? user) { user = null; return false; }
        public Task TryRemoveAsync(long id, bool notifyLeft = true, bool hotelView = false) => Task.CompletedTask;
        public ICollection<IRoomUser> GetAllWithRights() => [];
        public Task RunPeriodicCheckAsync() => Task.CompletedTask;
        public Task ProcessNewWalkRequestsAsync() => Task.CompletedTask;
        public void SetRoom(IRoomLogic room) { }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            _count = 0;
            return ValueTask.CompletedTask;
        }
    }

    private static RoomLogic CreateRoom(IRoomUserRepository userRepository) =>
        new(
            new RoomDto { Id = 1 },
            Mock.Of<IRoomTileMap>(),
            Mock.Of<IRoomPathFinder>(),
            userRepository,
            Mock.Of<IRoomBotRepository>(),
            Mock.Of<IRoomPetRepository>(),
            new InProcessRoomLock())
        {
            Name = "",
            Description = ""
        };

    private static Task SweepAsync(RoomLogic room, IRoomRepository repository) =>
        room.RunLockedAsync(async () =>
        {
            if (room.UserRepository.Count > 0)
            {
                return;
            }

            repository.TryRemove(room.Room.Id, out _);
            await room.DisposeAsync();
        });

    private static Task<bool> EnterAsync(RoomLogic room, CountingUserRepository users)
    {
        var entered = false;

        return room.RunLockedAsync(() =>
        {
            if (room.IsDisposed)
            {
                return Task.CompletedTask;
            }

            users.Join();
            entered = true;
            return Task.CompletedTask;
        }).ContinueWith(_ => entered, TaskContinuationOptions.ExecuteSynchronously);
    }

    [Test]
    public async Task EntryAndSweepRacing_NeverStrandsAPlayerInADisposedRoom()
    {
        for (var attempt = 0; attempt < 400; attempt++)
        {
            var users = new CountingUserRepository();
            var room = CreateRoom(users);
            var repository = new RoomRepository();
            repository.AddRoom(room);

            var enter = Task.Run(() => EnterAsync(room, users));
            var sweep = Task.Run(() => SweepAsync(room, repository));

            var entered = await enter;
            await sweep;

            if (entered)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(room.IsDisposed, Is.False,
                        "a room must not be torn down with a player who just joined it");
                    Assert.That(repository.TryGetRoomById(1), Is.SameAs(room),
                        "a room holding a player must stay registered, or the game loop stops ticking it");
                });
            }
            else
            {
                Assert.Multiple(() =>
                {
                    Assert.That(room.IsDisposed, Is.True);
                    Assert.That(repository.TryGetRoomById(1), Is.Null,
                        "a disposed room must not be left in the repository for the next loader to find");
                });
            }
        }
    }

    [Test]
    public async Task Sweep_RoomIsNeverUnregisteredWhileStillLive()
    {
        var users = new CountingUserRepository();
        var room = CreateRoom(users);
        var repository = new RoomRepository();
        repository.AddRoom(room);

        users.Join();

        await SweepAsync(room, repository);

        Assert.Multiple(() =>
        {
            Assert.That(room.IsDisposed, Is.False);
            Assert.That(users.Disposed, Is.False);
            Assert.That(repository.TryGetRoomById(1), Is.SameAs(room));
        });
    }

    [Test]
    public async Task Sweep_EmptyRoom_UnregistersAndDisposes()
    {
        var users = new CountingUserRepository();
        var room = CreateRoom(users);
        var repository = new RoomRepository();
        repository.AddRoom(room);

        await SweepAsync(room, repository);

        Assert.Multiple(() =>
        {
            Assert.That(room.IsDisposed, Is.True);
            Assert.That(users.Disposed, Is.True, "the room's occupant repositories must be released too");
            Assert.That(repository.TryGetRoomById(1), Is.Null);
        });
    }

    [Test]
    public async Task Entry_AfterDisposal_IsRefused()
    {
        var users = new CountingUserRepository();
        var room = CreateRoom(users);
        var repository = new RoomRepository();
        repository.AddRoom(room);

        await SweepAsync(room, repository);

        Assert.That(await EnterAsync(room, users), Is.False,
            "joining an unloaded room strands the player somewhere the game loop no longer ticks");
    }
}
