using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Filters;
using Ada.API.Interfaces.Networking.Events.Handlers;

namespace Ada.Networking.Events.Filters;

public class RateLimitPacketFilter(ILogger<RateLimitPacketFilter> logger) : INetworkPacketEventFilter
{
    private const double BurstCapacity = 60;
    private const double RefillPerSecond = 40;

    private static readonly double TicksPerSecond = Stopwatch.Frequency;
    private const long IdleEvictionSeconds = 120;
    private const int SweepEvery = 1000;

    private readonly ConcurrentDictionary<Guid, Bucket> _buckets = new();
    private int _callsSinceSweep;

    public Task<bool> AllowAsync(INetworkClient client, INetworkPacketEventHandler eventHandler)
    {
        var now = Stopwatch.GetTimestamp();
        var bucket = _buckets.GetOrAdd(client.Guid, _ => new Bucket(now));

        bool allowed;
        lock (bucket)
        {
            var elapsedSeconds = (now - bucket.LastRefill) / TicksPerSecond;
            bucket.LastRefill = now;
            bucket.Tokens = Math.Min(BurstCapacity, bucket.Tokens + elapsedSeconds * RefillPerSecond);

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

        if (!allowed)
        {
            logger.LogWarning("Rate limit exceeded for client {Guid}; dropping packet {Packet}",
                client.Guid, eventHandler.GetType().Name);
        }

        MaybeSweep(now);

        return Task.FromResult(allowed);
    }

    private void MaybeSweep(long now)
    {
        if (Interlocked.Increment(ref _callsSinceSweep) < SweepEvery)
        {
            return;
        }

        Interlocked.Exchange(ref _callsSinceSweep, 0);

        var cutoff = (long)(IdleEvictionSeconds * TicksPerSecond);

        foreach (var (guid, bucket) in _buckets)
        {
            long lastRefill;
            lock (bucket)
            {
                lastRefill = bucket.LastRefill;
            }

            if (now - lastRefill > cutoff)
            {
                _buckets.TryRemove(guid, out _);
            }
        }
    }

    private sealed class Bucket(long now)
    {
        public double Tokens = BurstCapacity;
        public long LastRefill = now;
    }
}
