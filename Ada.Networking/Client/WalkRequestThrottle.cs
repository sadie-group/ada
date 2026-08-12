using Ada.API.Interfaces.Networking.Client;
using Ada.Core.Shared;

namespace Ada.Networking.Client;

public class WalkRequestThrottle : IWalkRequestThrottle
{
    private readonly KeyedTokenBucket<long> _buckets =
        new(burstCapacity: 12, refillPerSecond: 8, idleEviction: TimeSpan.FromMinutes(5));

    public bool TryConsume(long playerId) => _buckets.TryConsume(playerId);

    public void Forget(long playerId) => _buckets.Forget(playerId);
}
