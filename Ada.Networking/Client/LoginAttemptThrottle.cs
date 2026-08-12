using System.Net;
using Ada.API.Interfaces.Networking.Client;
using Ada.Core.Shared;

namespace Ada.Networking.Client;

public class LoginAttemptThrottle : ILoginAttemptThrottle
{
    private readonly KeyedTokenBucket<IPAddress> _buckets =
        new(burstCapacity: 10, refillPerSecond: 0.2, idleEviction: TimeSpan.FromMinutes(10));

    public bool TryConsume(IPAddress address) => _buckets.TryConsume(address);
}
