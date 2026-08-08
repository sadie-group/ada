using Ada.API.Interfaces.Networking.Client;

namespace Ada.API.Interfaces.Networking.Events.Filters;

public interface IPreDispatchPacketFilter
{
    bool Allow(INetworkClient client, int packetId, Type handlerType);
}
