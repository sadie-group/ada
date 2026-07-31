using System.Collections.Concurrent;
using System.Linq.Expressions;
using Ada.API.Interfaces.Networking.Events.Handlers;

namespace Ada.Networking.Packets;

public class PacketHandlerFactory
{
    private readonly IServiceProvider _provider;
    private readonly ConcurrentDictionary<Type, Func<INetworkPacketEventHandler>> _factoryMap = new();

    public PacketHandlerFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    private Func<INetworkPacketEventHandler> BuildFactory(Type eventType)
    {
        var ctor = eventType.GetConstructors().Single();

        var args = ctor.GetParameters()
            .Select(p => Expression.Convert(
                Expression.Call(
                    Expression.Constant(_provider),
                    typeof(IServiceProvider).GetMethod("GetService")!,
                    Expression.Constant(p.ParameterType)
                ),
                p.ParameterType))
            .ToArray();

        var newExp = Expression.New(ctor, args);
        var lambda = Expression.Lambda<Func<INetworkPacketEventHandler>>(newExp);

        return lambda.Compile();
    }

    public INetworkPacketEventHandler Create(Type handlerType)
    {
        return _factoryMap.GetOrAdd(handlerType, BuildFactory)();
    }
}
