using System.Reflection;
using Ada.Networking.Packets;

namespace Ada.Networking;

public static class EventSerializer
{
    public static void SetPropertiesForEventHandler(object handler, NetworkPacketReader packetReader)
    {
        FillProperties(handler, ref packetReader);
    }

    private static void FillProperties(object target, ref NetworkPacketReader packetReader)
    {
        var properties = target.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(p => p.MetadataToken);

        foreach (var property in properties)
        {
            if (!property.CanWrite)
            {
                continue;
            }

            property.SetValue(target, ReadValue(property.PropertyType, ref packetReader), null);
        }
    }

    private static object ReadValue(Type type, ref NetworkPacketReader packetReader)
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
            return ReadAllStringDictionary(ref packetReader);
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            return ReadList(type.GetGenericArguments()[0], ref packetReader);
        }

        if (type is { IsClass: true, IsAbstract: false })
        {
            var instance = Activator.CreateInstance(type)
                ?? throw new Exception($"Cannot instantiate packet record type {type.FullName}");

            FillProperties(instance, ref packetReader);

            return instance;
        }

        throw new Exception($"Unsupported packet property type {type.FullName}");
    }

    private static object ReadList(Type elementType, ref NetworkPacketReader packetReader)
    {
        var count = packetReader.ReadInt();
        var list = (System.Collections.IList) Activator.CreateInstance(
            typeof(List<>).MakeGenericType(elementType))!;

        for (var i = 0; i < count; i++)
        {
            list.Add(ReadValue(elementType, ref packetReader));
        }

        return list;
    }

    private static Dictionary<string, string> ReadAllStringDictionary(ref NetworkPacketReader packetReader)
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
