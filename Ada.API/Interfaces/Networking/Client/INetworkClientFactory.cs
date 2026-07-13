using System.Net;
using System.Net.WebSockets;

namespace Ada.API.Interfaces.Networking.Client;

public interface INetworkClientFactory
{
    INetworkClient CreateClient(IPAddress ipAddress, Guid guid, WebSocket channel);
}