using System.Net;
using System.Net.WebSockets;
using Ada.API.Interfaces.Networking.Packets;

namespace Ada.API.Interfaces.Networking.Client;

public interface INetworkClientFactory
{
    INetworkClient CreateClient(IPAddress ipAddress, Guid guid, WebSocket channel);
    INetworkClient CreateClient(IPAddress ipAddress, Guid guid, WebSocket channel, IPacketCodec codec);
}
