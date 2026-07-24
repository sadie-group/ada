using System.Net;
using System.Net.WebSockets;
using Ada.API.Interfaces.Networking;

namespace Ada.API;

public interface INetworkObject
{
    Task WriteToStreamAsync(AbstractPacketWriter writer);
    Task WriteToStreamAsync(INetworkPacketWriter writer);
    void QueueOutbound(INetworkPacketWriter writer);
    Task FlushAsync();
    IPAddress IpAddress { get; set; } 
    Guid Guid { get; set; } 
    WebSocket WebSocket { get; set; } 
}