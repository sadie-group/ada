using System.Collections.Concurrent;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Ada.Networking.Packets;

public class PacketHandlerFactory
{
    private readonly IServiceProvider _provider;
    private readonly ConcurrentDictionary<Type, ObjectFactory> _factoryMap = new();

    public PacketHandlerFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    private static ObjectFactory BuildFactory(Type handlerType)
        => ActivatorUtilities.CreateFactory(handlerType, Type.EmptyTypes);

    public INetworkPacketEventHandler Create(Type handlerType)
    {
        return (INetworkPacketEventHandler) _factoryMap.GetOrAdd(handlerType, BuildFactory)(_provider, null);
    }
}
