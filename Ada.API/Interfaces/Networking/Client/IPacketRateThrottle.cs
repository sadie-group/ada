using System.Net;

namespace Ada.API.Interfaces.Networking.Client;

public interface IPacketRateThrottle
{
    bool TryConsume(Guid clientGuid, IPAddress address);

    void Forget(Guid clientGuid);
}
