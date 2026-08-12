using System.Net;

namespace Ada.API.Interfaces.Networking.Client;

public interface ILoginAttemptThrottle
{
    bool TryConsume(IPAddress address);
}
