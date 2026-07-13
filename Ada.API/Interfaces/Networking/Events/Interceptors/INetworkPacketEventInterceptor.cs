using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;

namespace Ada.API.Interfaces.Networking.Events.Interceptors;

public interface INetworkPacketEventInterceptor<in TPacket> where TPacket : INetworkPacketEventHandler
{
    Task InterceptAsync(INetworkClient client, TPacket packet);
}