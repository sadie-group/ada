using System.Net;
using System.Net.WebSockets;
using Ada.API.Interfaces.Networking;

namespace Ada.API;

public interface INetworkObject
{
    Task WriteToStreamAsync(AbstractPacketWriter writer);
    Task WriteToStreamAsync(INetworkPacketWriter writer);
    List<INetworkPacketWriter> Outbox { get; set; }
    IPAddress IpAddress { get; set; } 
    Guid Guid { get; set; } 
    WebSocket WebSocket { get; set; } 
}