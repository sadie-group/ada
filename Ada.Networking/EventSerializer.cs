using System.Collections.Concurrent;
using System.Reflection;
using Ada.API.Interfaces.Networking.Packets;
using Ada.Networking.Packets.Serialization;

namespace Ada.Networking;

public static class EventSerializer
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> WritableProperties = new();

    public static void SetPropertiesForEventHandler(object handler, INetworkPacketReader packetReader)
    {
        FillProperties(handler, packetReader);
    }

    private static PropertyInfo[] GetWritableProperties(Type type)
        => WritableProperties.GetOrAdd(type, t => t
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .OrderBy(p => p.MetadataToken)
            .ToArray());

    private static void FillProperties(object target, INetworkPacketReader packetReader)
    {
        foreach (var property in GetWritableProperties(target.GetType()))
        {
            PropertyAccessorCache.SetValue(property, target, ReadValue(property.PropertyType, packetReader));
        }
    }

    private static object ReadValue(Type type, INetworkPacketReader packetReader)
    {
        if (type == typeof(int))
        {
            return packetReader.ReadInt();
        }

        if (type == typeof(long))
        {
            return (long) packetReader.ReadInt();
        }

        if (type == typeof(string))
        {
            return packetReader.ReadString();
        }

        if (type == typeof(bool))
        {
            return packetReader.ReadBool();
        }

        if (type == typeof(Dictionary<string, string>))
        {
            return ReadAllStringDictionary(packetReader);
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            return ReadList(type.GetGenericArguments()[0], packetReader);
        }

        if (type is { IsClass: true, IsAbstract: false })
        {
            var instance = Activator.CreateInstance(type)
                ?? throw new Exception($"Cannot instantiate packet record type {type.FullName}");

            FillProperties(instance, packetReader);

            return instance;
        }

        throw new Exception($"Unsupported packet property type {type.FullName}");
    }

    private static object ReadList(Type elementType, INetworkPacketReader packetReader)
    {
        var count = packetReader.ReadInt();
        var list = (System.Collections.IList) Activator.CreateInstance(
            typeof(List<>).MakeGenericType(elementType))!;

        for (var i = 0; i < count; i++)
        {
            list.Add(ReadValue(elementType, packetReader));
        }

        return list;
    }

    private static Dictionary<string, string> ReadAllStringDictionary(INetworkPacketReader packetReader)
    {
        var temp = new Dictionary<string, string>();
        var amount = packetReader.ReadInt();

        for (var i = 0; i < amount / 2; i++)
        {
            temp[packetReader.ReadString()] = packetReader.ReadString();
        }

        return temp;
    }
}
