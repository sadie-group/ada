using System.Net;
using System.Net.WebSockets;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Packets;

namespace Ada.API;

public interface INetworkObject
{
    IPacketCodec Codec { get; set; }
    Task WriteToStreamAsync(AbstractPacketWriter writer);
    Task WriteToStreamAsync(INetworkPacketWriter writer);
    void QueueOutbound(INetworkPacketWriter writer);
    Task FlushAsync();
    IPAddress IpAddress { get; set; } 
    Guid Guid { get; set; } 
    WebSocket WebSocket { get; set; } 
}