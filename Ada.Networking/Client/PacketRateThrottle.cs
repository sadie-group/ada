using System.Net;
using Ada.API.Interfaces.Networking.Client;
using Ada.Core.Shared;

namespace Ada.Networking.Client;

public class PacketRateThrottle : IPacketRateThrottle
{
    private readonly KeyedTokenBucket<Guid> _perConnection =
        new(burstCapacity: 120, refillPerSecond: 60, idleEviction: TimeSpan.FromMinutes(5));

    private readonly KeyedTokenBucket<IPAddress> _perAddress =
        new(burstCapacity: 480, refillPerSecond: 240, idleEviction: TimeSpan.FromMinutes(5));

    public bool TryConsume(Guid clientGuid, IPAddress address)
        => _perConnection.TryConsume(clientGuid) && _perAddress.TryConsume(address);

    public void Forget(Guid clientGuid) => _perConnection.Forget(clientGuid);
}
