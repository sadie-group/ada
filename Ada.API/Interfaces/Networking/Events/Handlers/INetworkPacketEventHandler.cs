using Ada.API.Interfaces.Networking.Client;

namespace Ada.API.Interfaces.Networking.Events.Handlers;

public interface INetworkPacketEventHandler
{
    Task HandleAsync(INetworkClient client);
}