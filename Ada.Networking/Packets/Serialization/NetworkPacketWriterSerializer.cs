using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Packets;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Packets.Serialization
{
    public static class NetworkPacketWriterSerializer
    {
        private static readonly Dictionary<Type, Action<object, INetworkPacketWriter>> primitiveWriters =
            new()
            {
                { typeof(string), (v, w) => w.WriteString((string)v) },
                { typeof(int), (v, w) => w.WriteInteger((int)v) },
                { typeof(short), (v, w) => w.WriteShort((short)v) },
                { typeof(long), (v, w) => w.WriteLong((long)v) },
                { typeof(bool), (v, w) => w.WriteBool((bool)v) }
            };

        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> propertyCache = new();
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> attributedPropertyCache = new();
        private static readonly ConcurrentDictionary<Type, Action<object, INetworkPacketWriter>?> onSerializeCache = new();

        private static PropertyInfo[] GetCachedProperties(Type type)
        {
            return propertyCache.GetOrAdd(type, static t => t.GetProperties()
                .Where(p => p.DeclaringType != typeof(AbstractPacketWriter))
                .OrderBy(p => p.MetadataToken)
                .ToArray());
        }

        private static bool InvokeOnSerializeIfExists(object packet, INetworkPacketWriter writer)
        {
            var invoker = onSerializeCache.GetOrAdd(packet.GetType(), static t =>
            {
                var method = t.GetMethod("OnSerialize");

                if (method == null || method.GetBaseDefinition().DeclaringType == method.DeclaringType)
                {
                    return null;
                }

                var packetParam = Expression.Parameter(typeof(object));
                var writerParam = Expression.Parameter(typeof(INetworkPacketWriter));

                return Expression.Lambda<Action<object, INetworkPacketWriter>>(
                    Expression.Call(Expression.Convert(packetParam, t), method, writerParam),
                    packetParam,
                    writerParam).Compile();
            });

            if (invoker == null)
            {
                return false;
            }

            invoker(packet, writer);
            return true;
        }

        private static short GetPacketIdentifier(object packetObject, IPacketCodec codec)
        {
            if (codec.IdMap.TryGetOutgoingId(packetObject.GetType(), out var mapped))
            {
                return mapped;
            }

            throw new InvalidOperationException(
                $"Packet type {packetObject.GetType()} has no outgoing id for revision '{codec.Revision}'"
            );
        }

        private static bool TryWritePrimitive(PropertyInfo property, object packet, INetworkPacketWriter writer)
        {
            if (primitiveWriters.TryGetValue(property.PropertyType, out var action))
            {
                var value = PropertyAccessorCache.GetValue(property, packet);

                if (value == null)
                {
                    writer.WriteString(string.Empty);
                    return true;
                }

                action(value, writer);
                return true;
            }

            return false;
        }

        private static void WriteDictionary<TKey, TValue>(
            Dictionary<TKey, TValue> dict,
            INetworkPacketWriter writer,
            Action<TKey> keyWriter,
            Action<TValue> valueWriter
        )
            where TKey : notnull
        {
            writer.WriteInteger(dict.Count);

            foreach (var kv in dict)
            {
                keyWriter(kv.Key);
                valueWriter(kv.Value);
            }
        }

        private static void AddObjectToWriter(object packet, INetworkPacketWriter writer, bool needsAttribute = false)
        {
            var props = needsAttribute
                ? attributedPropertyCache.GetOrAdd(packet.GetType(), static t =>
                    GetCachedProperties(t).Where(p => Attribute.IsDefined(p, typeof(PacketDataAttribute))).ToArray())
                : GetCachedProperties(packet.GetType());

            foreach (var property in props)
            {
                WriteProperty(property, writer, packet);
            }
        }

        public static INetworkPacketWriter Serialize(object packet, IPacketCodec codec)
        {
            var writer = codec.CreateWriter();

            writer.WriteShort(GetPacketIdentifier(packet, codec));

            return SerializeBody(packet, writer);
        }

        private static INetworkPacketWriter SerializeBody(object packet, INetworkPacketWriter writer)
        {
            if (InvokeOnSerializeIfExists(packet, writer))
            {
                return writer;
            }

            if (PacketWriterFastPath.Handler is { } fastPath && fastPath(packet, writer))
            {
                return writer;
            }

            AddObjectToWriter(packet, writer);

            return writer;
        }

        private static void WriteStringListPropertyToWriter(List<string> list, INetworkPacketWriter writer)
        {
            writer.WriteInteger(list.Count);

            foreach (var s in list)
            {
                writer.WriteString(s ?? "");
            }
        }

        private static void WriteArbitraryListPropertyToWriter(PropertyInfo property, INetworkPacketWriter writer, object packet)
        {
            var collection = (ICollection)PropertyAccessorCache.GetValue(property, packet)!;

            writer.WriteInteger(collection.Count);

            foreach (var item in collection)
            {
                var props = GetCachedProperties(item.GetType());
                foreach (var p in props)
                {
                    WriteProperty(p, writer, item);
                }
            }
        }

        private static void WriteProperty(PropertyInfo property, INetworkPacketWriter writer, object packet)
        {
            if (TryWritePrimitive(property, packet, writer))
            {
                return;
            }

            var type = property.PropertyType;
            var value = PropertyAccessorCache.GetValue(property, packet);

            if (type == typeof(List<string>))
            {
                WriteStringListPropertyToWriter(value as List<string> ?? new(), writer);
                return;
            }

            if (type == typeof(Dictionary<int, string>))
            {
                WriteDictionary(value as Dictionary<int, string> ?? new(), writer, writer.WriteInteger, v => writer.WriteString(v ?? ""));
                return;
            }

            if (type == typeof(Dictionary<long, string>))
            {
                WriteDictionary(value as Dictionary<long, string> ?? new(), writer, writer.WriteLong, v => writer.WriteString(v ?? ""));
                return;
            }

            if (type == typeof(Dictionary<int, long>))
            {
                WriteDictionary(value as Dictionary<int, long> ?? new(), writer, writer.WriteInteger, writer.WriteLong);
                return;
            }

            if (type == typeof(Dictionary<int, List<string>>))
            {
                var dict = value as Dictionary<int, List<string>> ?? new();

                writer.WriteInteger(dict.Count);
                foreach (var kv in dict)
                {
                    writer.WriteInteger(kv.Key);
                    foreach (var s in kv.Value)
                    {
                        writer.WriteString(s);
                    }
                }
                return;
            }

            if (type == typeof(Dictionary<string, int>))
            {
                WriteDictionary(value as Dictionary<string, int> ?? new(), writer, writer.WriteString, writer.WriteInteger);
                return;
            }

            if (type == typeof(Dictionary<string, string>))
            {
                WriteDictionary(value as Dictionary<string, string> ?? new(), writer, writer.WriteString, writer.WriteString);
                return;
            }

            if (value is ICollection collection)
            {
                var element = type.IsArray
                    ? type.GetElementType()
                    : type.IsGenericType
                        ? type.GetGenericArguments().FirstOrDefault()
                        : null;

                if (element == typeof(string))
                {
                    writer.WriteInteger(collection.Count);

                    foreach (var item in collection)
                    {
                        writer.WriteString(item as string ?? "");
                    }

                    return;
                }

                if (element != null && primitiveWriters.TryGetValue(element, out var writeElement))
                {
                    writer.WriteInteger(collection.Count);

                    foreach (var item in collection)
                    {
                        writeElement(item!, writer);
                    }

                    return;
                }

                WriteArbitraryListPropertyToWriter(property, writer, packet);
                return;
            }

            if (value == null)
            {
                return;
            }

            AddObjectToWriter(value, writer, true);
        }
    }
}
