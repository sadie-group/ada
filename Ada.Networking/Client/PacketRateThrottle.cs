using Ada.API.Interfaces.Networking.Client;
using Ada.Core.Shared;

namespace Ada.Networking.Client;

public class PacketRateThrottle : IPacketRateThrottle
{
    private readonly KeyedTokenBucket<Guid> _buckets =
        new(burstCapacity: 120, refillPerSecond: 60, idleEviction: TimeSpan.FromMinutes(5));

    public bool TryConsume(Guid clientGuid) => _buckets.TryConsume(clientGuid);

    public void Forget(Guid clientGuid) => _buckets.Forget(clientGuid);
}
