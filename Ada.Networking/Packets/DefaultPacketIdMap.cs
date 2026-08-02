using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.API.Interfaces.Networking.Packets;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Packets;

public sealed class DefaultPacketIdMap(Func<IEnumerable<Assembly>> assemblySource) : IPacketIdMap
{
    private readonly Lazy<Tables> _tables = new(() => Build(assemblySource()), LazyThreadSafetyMode.ExecutionAndPublication);

    public DefaultPacketIdMap()
        : this(() => AppDomain.CurrentDomain.GetAssemblies())
    {
    }

    public bool TryGetHandlerType(short packetId, [NotNullWhen(true)] out Type? handlerType)
        => _tables.Value.Handlers.TryGetValue(packetId, out handlerType);

    public bool TryGetOutgoingId(Type writerType, out short packetId)
        => _tables.Value.Outgoing.TryGetValue(writerType, out packetId);

    private sealed record Tables(
        FrozenDictionary<short, Type> Handlers,
        FrozenDictionary<Type, short> Outgoing);

    private static Tables Build(IEnumerable<Assembly> assemblies)
    {
        var handlers = new Dictionary<short, Type>();
        var outgoing = new Dictionary<Type, short>();

        foreach (var type in EnumerateTypes(assemblies))
        {
            var attribute = type.GetCustomAttribute<PacketIdAttribute>(false);

            if (attribute == null)
            {
                continue;
            }

            if (typeof(INetworkPacketEventHandler).IsAssignableFrom(type))
            {
                handlers[attribute.Id] = type;
                continue;
            }

            if (typeof(AbstractPacketWriter).IsAssignableFrom(type))
            {
                outgoing[type] = attribute.Id;
            }
        }

        return new Tables(handlers.ToFrozenDictionary(), outgoing.ToFrozenDictionary());
    }

    private static IEnumerable<Type> EnumerateTypes(IEnumerable<Assembly> assemblies)
    {
        foreach (var assembly in assemblies)
        {
            Type?[] types;

            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types;
            }

            foreach (var type in types)
            {
                if (type is { IsClass: true, IsAbstract: false })
                {
                    yield return type;
                }
            }
        }
    }
}
