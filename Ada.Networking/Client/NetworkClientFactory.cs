using System.Net;
using System.Net.WebSockets;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Packets;
using Microsoft.Extensions.DependencyInjection;

namespace Ada.Networking.Client;

public class NetworkClientFactory(IServiceProvider serviceProvider) : INetworkClientFactory
{
    public INetworkClient CreateClient(IPAddress ipAddress, Guid guid, WebSocket webSocket)
    {
        return ActivatorUtilities.CreateInstance<NetworkClient>(serviceProvider, ipAddress, guid, webSocket);
    }

    public INetworkClient CreateClient(IPAddress ipAddress, Guid guid, WebSocket webSocket, IPacketCodec codec)
    {
        var client = CreateClient(ipAddress, guid, webSocket);
        client.Codec = codec;

        return client;
    }
}