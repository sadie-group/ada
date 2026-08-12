using Ada.Core.Shared;

namespace Ada.Tests.Core;

[TestFixture]
public class KeyedTokenBucketTests
{
    [Test]
    public void Consume_AllowsTheBurstThenRefuses()
    {
        var bucket = new KeyedTokenBucket<int>(burstCapacity: 3, refillPerSecond: 0, idleEviction: TimeSpan.FromMinutes(1));

        Assert.Multiple(() =>
        {
            Assert.That(bucket.TryConsume(1), Is.True);
            Assert.That(bucket.TryConsume(1), Is.True);
            Assert.That(bucket.TryConsume(1), Is.True);
            Assert.That(bucket.TryConsume(1), Is.False, "the burst is three");
        });
    }

    [Test]
    public void Consume_KeepsKeysIndependent()
    {
        var bucket = new KeyedTokenBucket<int>(burstCapacity: 1, refillPerSecond: 0, idleEviction: TimeSpan.FromMinutes(1));

        Assert.Multiple(() =>
        {
            Assert.That(bucket.TryConsume(1), Is.True);
            Assert.That(bucket.TryConsume(1), Is.False);
            Assert.That(bucket.TryConsume(2), Is.True, "one key exhausting its budget must not affect another");
        });
    }

    [Test]
    public void Forget_ResetsTheKey()
    {
        var bucket = new KeyedTokenBucket<int>(burstCapacity: 1, refillPerSecond: 0, idleEviction: TimeSpan.FromMinutes(1));

        bucket.TryConsume(1);
        Assert.That(bucket.TryConsume(1), Is.False);

        bucket.Forget(1);

        Assert.That(bucket.TryConsume(1), Is.True);
    }

    [Test]
    public void Consume_OnAnEstablishedKey_DoesNotAllocate()
    {
        var bucket = new KeyedTokenBucket<int>(
            burstCapacity: 1_000_000,
            refillPerSecond: 1_000_000,
            idleEviction: TimeSpan.FromMinutes(10));

        for (var i = 0; i < 100; i++)
        {
            bucket.TryConsume(1);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();

        var allowed = 0;

        for (var i = 0; i < 500; i++)
        {
            if (bucket.TryConsume(1))
            {
                allowed++;
            }
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Multiple(() =>
        {
            Assert.That(allowed, Is.EqualTo(500));
            Assert.That(allocated, Is.Zero,
                "this runs twice for every packet, so the lookup must not allocate a closure per call");
        });
    }
}
