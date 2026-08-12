using Ada.Game.Rooms.Wired;

namespace Ada.Tests.Game.Rooms.Wired;

[TestFixture]
public class WiredTimerServiceTests
{
    [Test]
    public void RetainOnly_DropsStateForUnloadedRooms()
    {
        var service = new WiredTimerService();

        service.Reset(1);
        service.Reset(2);
        service.TryMarkFired(1, itemId: 100);
        service.TryMarkFired(2, itemId: 100);

        var released = service.RetainOnly(new HashSet<long> { 1 });

        Assert.Multiple(() =>
        {
            Assert.That(released, Is.EqualTo(1), "room 2 is no longer loaded");
            Assert.That(service.TryMarkFired(2, itemId: 100), Is.True);
            Assert.That(service.TryMarkFired(1, itemId: 100), Is.False);
        });
    }

    [Test]
    public void RetainOnly_NothingLoaded_ReleasesEverything()
    {
        var service = new WiredTimerService();

        for (var roomId = 1; roomId <= 25; roomId++)
        {
            service.Reset(roomId);
            service.TryMarkFired(roomId, itemId: 1);
        }

        Assert.That(service.RetainOnly(new HashSet<long>()), Is.EqualTo(25));
    }

    [Test]
    public void RetainOnly_EverythingLoaded_ReleasesNothing()
    {
        var service = new WiredTimerService();

        service.Reset(1);
        service.Reset(2);

        Assert.That(service.RetainOnly(new HashSet<long> { 1, 2 }), Is.Zero);
    }

    [Test]
    public void RetainOnly_IsSafeAlongsideConcurrentUse()
    {
        var service = new WiredTimerService();

        Assert.DoesNotThrow(() => Parallel.For(0, 200, i =>
        {
            var roomId = i % 20;

            service.GetElapsed(roomId);
            service.TryMarkFired(roomId, i);
            service.RetainOnly(new HashSet<long> { 0, 1, 2, 3, 4 });
        }));
    }
}
