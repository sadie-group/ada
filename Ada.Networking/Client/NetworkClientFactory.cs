using System.Net;
using System.Net.WebSockets;
using Ada.API.Interfaces.Networking.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Ada.Networking.Client;

public class NetworkClientFactory(IServiceProvider serviceProvider) : INetworkClientFactory
{
    public INetworkClient CreateClient(IPAddress ipAddress, Guid guid, WebSocket webSocket)
    {
        return ActivatorUtilities.CreateInstance<NetworkClient>(serviceProvider, ipAddress, guid, webSocket);
    }
}