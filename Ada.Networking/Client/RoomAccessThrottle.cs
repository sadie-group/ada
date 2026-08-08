using Ada.API.Interfaces.Networking.Client;
using Ada.Core.Shared;

namespace Ada.Networking.Client;

public class RoomAccessThrottle : IRoomAccessThrottle
{
    private readonly KeyedTokenBucket<(long PlayerId, long RoomId)> _buckets =
        new(burstCapacity: 5, refillPerSecond: 0.1, idleEviction: TimeSpan.FromMinutes(30));

    public bool TryConsume(long playerId, long roomId) => _buckets.TryConsume((playerId, roomId));
}
