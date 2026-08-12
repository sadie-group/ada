using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;

namespace Ada.API.Interfaces.Networking.Events.Filters;

public interface INetworkPacketEventFilter
{
    Task<bool> AllowAsync(INetworkClient client, INetworkPacketEventHandler eventHandler);
}
