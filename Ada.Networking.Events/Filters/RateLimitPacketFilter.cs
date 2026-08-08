using System.Collections.Concurrent;
using System.Diagnostics;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Filters;
using Microsoft.Extensions.Logging;

namespace Ada.Networking.Events.Filters;

public class RateLimitPacketFilter(ILogger<RateLimitPacketFilter> logger) : IPreDispatchPacketFilter
{
    private const double _burstCapacity = 60;
    private const double _refillPerSecond = 40;

    private static readonly double _ticksPerSecond = Stopwatch.Frequency;
    private const long _idleEvictionSeconds = 120;
    private const int _sweepEvery = 1000;

    private readonly ConcurrentDictionary<Guid, Bucket> _buckets = new();
    private int _callsSinceSweep;

    public bool Allow(INetworkClient client, int packetId, Type handlerType)
    {
        var now = Stopwatch.GetTimestamp();
        var bucket = _buckets.GetOrAdd(client.Guid, _ => new Bucket(now));

        bool allowed;
        lock (bucket)
        {
            var elapsedSeconds = (now - bucket.LastRefill) / _ticksPerSecond;
            bucket.LastRefill = now;
            bucket.Tokens = Math.Min(_burstCapacity, bucket.Tokens + elapsedSeconds * _refillPerSecond);

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
            logger.LogWarning("Rate limit exceeded for client {Guid}; dropping packet {PacketId} ({Packet})",
                client.Guid, packetId, handlerType.Name);
        }

        MaybeSweep(now);

        return allowed;
    }

    private void MaybeSweep(long now)
    {
        if (Interlocked.Increment(ref _callsSinceSweep) < _sweepEvery)
        {
            return;
        }

        Interlocked.Exchange(ref _callsSinceSweep, 0);

        var cutoff = (long)(_idleEvictionSeconds * _ticksPerSecond);

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
        public double Tokens = _burstCapacity;
        public long LastRefill = now;
    }
}
