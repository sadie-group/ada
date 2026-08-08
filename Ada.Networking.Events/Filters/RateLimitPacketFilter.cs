using System.Net;
using Ada.API.Interfaces.Networking.Client;
using Ada.Core.Shared;
using Ada.API.Interfaces.Networking.Events.Filters;
using Microsoft.Extensions.Logging;

namespace Ada.Networking.Events.Filters;

public class RateLimitPacketFilter(ILogger<RateLimitPacketFilter> logger) : IPreDispatchPacketFilter
{
    private readonly KeyedTokenBucket<long> _playerBuckets =
        new(burstCapacity: 60, refillPerSecond: 40, idleEviction: TimeSpan.FromMinutes(2));

    private readonly KeyedTokenBucket<IPAddress> _addressBuckets =
        new(burstCapacity: 120, refillPerSecond: 60, idleEviction: TimeSpan.FromMinutes(2));

    public bool Allow(INetworkClient client, int packetId, Type handlerType)
    {
        var allowed = client.Player is { } player
            ? _playerBuckets.TryConsume(player.Player.Id) && _addressBuckets.TryConsume(client.IpAddress)
            : _addressBuckets.TryConsume(client.IpAddress);

        if (!allowed)
        {
            logger.LogWarning("Rate limit exceeded for client {Guid}; dropping packet {PacketId} ({Packet})",
                client.Guid, packetId, handlerType.Name);
        }

        return allowed;
    }
}
