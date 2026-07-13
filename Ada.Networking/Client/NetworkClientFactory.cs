using System.Net;
using System.Net.WebSockets;
using Microsoft.Extensions.DependencyInjection;
using Ada.API.Interfaces.Networking.Client;

namespace Ada.Networking.Client;

public class NetworkClientFactory(IServiceProvider serviceProvider) : INetworkClientFactory
{
    public INetworkClient CreateClient(IPAddress ipAddress, Guid guid, WebSocket webSocket)
    {
        return ActivatorUtilities.CreateInstance<NetworkClient>(serviceProvider, ipAddress, guid, webSocket);
    }
}