using Ada.API.DTOs.Rooms;
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
public class RoomLockConcurrencyTests
{
    private static RoomLogic CreateLogic()
        => new(
            new RoomDto(),
            Mock.Of<IRoomTileMap>(),
            Mock.Of<IRoomPathFinder>(),
            Mock.Of<IRoomUserRepository>(),
            Mock.Of<IRoomBotRepository>(),
            Mock.Of<IRoomPetRepository>(),
            new InProcessRoomLock())
        {
            Name = "",
            Description = ""
        };

    private static async Task<int> CountDetectionsAsync(Func<RoomLogic, Task> body)
    {
        var detections = 0;

        void OnDetected(int roomId, int active) => Interlocked.Increment(ref detections);

        RoomLogic.ConcurrentReentryDetected += OnDetected;

        try
        {
            await body(CreateLogic());
        }
        finally
        {
            RoomLogic.ConcurrentReentryDetected -= OnDetected;
        }

        return detections;
    }

    [Test]
    public async Task SequentialNesting_DoesNotReportConcurrentReentry()
    {
        var detections = await CountDetectionsAsync(async room =>
            await room.RunLockedAsync(async () =>
                await room.RunLockedAsync(async () =>
                    await room.RunLockedAsync(() => Task.CompletedTask))));

        Assert.That(detections, Is.Zero,
            "Nested reentrant calls run one at a time, so none of them is a concurrency violation.");
    }

    [Test]
    public async Task ConcurrentReentry_InsideLockedRegion_IsReported()
    {
        var detections = await CountDetectionsAsync(async room =>
        {
            await room.RunLockedAsync(async () =>
            {
                using var bothInside = new SemaphoreSlim(0, 2);
                using var release = new SemaphoreSlim(0, 2);

                async Task BranchAsync()
                {
                    await room.RunLockedAsync(async () =>
                    {
                        bothInside.Release();
                        await release.WaitAsync();
                    });
                }

                var first = Task.Run(BranchAsync);
                var second = Task.Run(BranchAsync);

                await bothInside.WaitAsync();
                await bothInside.WaitAsync();

                release.Release(2);

                await Task.WhenAll(first, second);
            });
        });

        Assert.That(detections, Is.GreaterThan(0),
            "Two branches were inside the same room lock at once and nothing said so.");
    }

    [Test]
    public async Task ConcurrentReentry_StillRunsBothBodies()
    {
        var ran = 0;

        await CountDetectionsAsync(async room =>
        {
            await room.RunLockedAsync(async () =>
            {
                await Task.WhenAll(
                    room.RunLockedAsync(() =>
                    {
                        Interlocked.Increment(ref ran);
                        return Task.CompletedTask;
                    }),
                    room.RunLockedAsync(() =>
                    {
                        Interlocked.Increment(ref ran);
                        return Task.CompletedTask;
                    }));
            });
        });

        Assert.That(ran, Is.EqualTo(2));
    }

    [Test]
    public async Task LockHeldForMilliseconds_IsZeroWhenFree()
    {
        var room = CreateLogic();

        Assert.That(room.LockHeldForMilliseconds, Is.Zero);

        await room.RunLockedAsync(() => Task.CompletedTask);

        Assert.That(room.LockHeldForMilliseconds, Is.Zero,
            "The lock was released, so the game loop must not report it as stuck.");
    }

    [Test]
    public async Task LockHeldForMilliseconds_IsNonZeroWhileHeld()
    {
        var room = CreateLogic();

        long observed = 0;

        await room.RunLockedAsync(async () =>
        {
            await Task.Delay(30);
            observed = room.LockHeldForMilliseconds;
        });

        Assert.That(observed, Is.GreaterThan(0),
            "A held lock has to be visible to the stuck-room sampling in the game loop.");
    }
}
