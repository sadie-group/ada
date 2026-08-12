using System.Collections.Concurrent;
using System.Diagnostics;

namespace Ada.Core.Shared;

public sealed class KeyedTokenBucket<TKey>(double burstCapacity, double refillPerSecond, TimeSpan idleEviction)
    where TKey : notnull
{
    private static readonly double _ticksPerSecond = Stopwatch.Frequency;
    private const int _sweepEvery = 1000;

    private readonly ConcurrentDictionary<TKey, Bucket> _buckets = new();
    private readonly long _idleEvictionTicks = (long) (idleEviction.TotalSeconds * _ticksPerSecond);
    private int _callsSinceSweep;

    public bool TryConsume(TKey key)
    {
        var now = Stopwatch.GetTimestamp();

        if (!_buckets.TryGetValue(key, out var bucket))
        {
            bucket = _buckets.GetOrAdd(
                key,
                static (_, state) => new Bucket(state.Now, state.Capacity),
                (Now: now, Capacity: burstCapacity));
        }

        bool allowed;

        lock (bucket)
        {
            var elapsedSeconds = (now - bucket.LastRefill) / _ticksPerSecond;

            bucket.LastRefill = now;
            bucket.Tokens = Math.Min(burstCapacity, bucket.Tokens + elapsedSeconds * refillPerSecond);

            if (bucket.Tokens >= 1)
            {
                bucket.Tokens -= 1;
                allowed = true;
            }
            else
            {
                allowed = false;
            }
        }

        MaybeSweep(now);

        return allowed;
    }

    public void Forget(TKey key) => _buckets.TryRemove(key, out _);

    private void MaybeSweep(long now)
    {
        if (Interlocked.Increment(ref _callsSinceSweep) < _sweepEvery)
        {
            return;
        }

        Interlocked.Exchange(ref _callsSinceSweep, 0);

        foreach (var (key, bucket) in _buckets)
        {
            long lastRefill;

            lock (bucket)
            {
                lastRefill = bucket.LastRefill;
            }

            if (now - lastRefill > _idleEvictionTicks)
            {
                _buckets.TryRemove(key, out _);
            }
        }
    }

    private sealed class Bucket(long now, double tokens)
    {
        public double Tokens = tokens;
        public long LastRefill = now;
    }
}
