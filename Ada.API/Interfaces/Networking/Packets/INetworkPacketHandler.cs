using Ada.API.Interfaces.Networking.Client;

namespace Ada.API.Interfaces.Networking.Packets;

public interface INetworkPacketHandler
{ 
    Task HandleAsync(INetworkClient client, INetworkPacket packet);
}