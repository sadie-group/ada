using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Packets.Serialization
{
    public static class NetworkPacketWriterSerializer
    {
        private static readonly Dictionary<Type, Action<object, NetworkPacketWriter>> primitiveWriters =
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
        private static readonly ConcurrentDictionary<Type, short> packetIdCache = new();
        private static readonly ConcurrentDictionary<Type, Action<object>?> onConfigureRulesCache = new();
        private static readonly ConcurrentDictionary<Type, Action<object, NetworkPacketWriter>?> onSerializeCache = new();

        private static PropertyInfo[] GetCachedProperties(Type type)
        {
            // The rule dictionaries declared on AbstractPacketWriter are serializer
            // configuration, not packet fields.
            return propertyCache.GetOrAdd(type, static t => t.GetProperties()
                .Where(p => p.DeclaringType != typeof(AbstractPacketWriter))
                .ToArray());
        }

        private static void InvokeOnConfigureRules(object packet)
        {
            var invoker = onConfigureRulesCache.GetOrAdd(packet.GetType(), static t =>
            {
                var method = t.GetMethod("OnConfigureRules");

                if (method == null || method.GetBaseDefinition().DeclaringType == method.DeclaringType)
                {
                    return null;
                }

                var packetParam = Expression.Parameter(typeof(object));

                return Expression.Lambda<Action<object>>(
                    Expression.Call(Expression.Convert(packetParam, t), method),
                    packetParam).Compile();
            });

            invoker?.Invoke(packet);
        }

        private static bool InvokeOnSerializeIfExists(object packet, NetworkPacketWriter writer)
        {
            var invoker = onSerializeCache.GetOrAdd(packet.GetType(), static t =>
            {
                var method = t.GetMethod("OnSerialize");

                if (method == null || method.GetBaseDefinition().DeclaringType == method.DeclaringType)
                {
                    return null;
                }

                var packetParam = Expression.Parameter(typeof(object));
                var writerParam = Expression.Parameter(typeof(NetworkPacketWriter));

                return Expression.Lambda<Action<object, NetworkPacketWriter>>(
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

        private static short GetPacketIdentifierFromAttribute(object packetObject)
        {
            return packetIdCache.GetOrAdd(packetObject.GetType(), static t =>
            {
                var attr = t.GetCustomAttribute<PacketIdAttribute>();

                if (attr == null)
                {
                    throw new InvalidOperationException(
                        $"Missing packet identifier attribute for packet type {t}"
                    );
                }

                return attr.Id;
            });
        }

        private static bool TryWritePrimitive(PropertyInfo property, object packet, NetworkPacketWriter writer)
        {
            if (primitiveWriters.TryGetValue(property.PropertyType, out var action))
            {
                var value = PropertyAccessorCache.GetValue(property, packet);
                action(value, writer);
                return true;
            }

            return false;
        }

        private static void WriteDictionary<TKey, TValue>(
            Dictionary<TKey, TValue> dict,
            NetworkPacketWriter writer,
            Action<TKey> keyWriter,
            Action<TValue> valueWriter
        )
        {
            writer.WriteInteger(dict.Count);

            foreach (var kv in dict)
            {
                keyWriter(kv.Key);
                valueWriter(kv.Value);
            }
        }

        private static void AddObjectToWriter(object packet, NetworkPacketWriter writer, bool needsAttribute = false)
        {
            var props = needsAttribute
                ? attributedPropertyCache.GetOrAdd(packet.GetType(), static t =>
                    GetCachedProperties(t).Where(p => Attribute.IsDefined(p, typeof(PacketDataAttribute))).ToArray())
                : GetCachedProperties(packet.GetType());

            var abstractWriter  = packet as AbstractPacketWriter;
            var conversionRules = abstractWriter?.ConversionRules;
            var insteadRules    = abstractWriter?.InsteadRulesSerialize;
            var afterRules      = abstractWriter?.AfterRulesSerialize;

            foreach (var property in props)
            {
                if (conversionRules != null && conversionRules.TryGetValue(property.Name, out var conv))
                {
                    var raw = PropertyAccessorCache.GetValue(property, packet);
                    var converted = conv.Value(raw);
                    WriteType(conv.Key, converted, writer);
                    continue;
                }

                if (insteadRules != null && insteadRules.TryGetValue(property.Name, out var instead))
                {
                    instead(writer);
                    continue;
                }

                WriteProperty(property, writer, packet);

                if (afterRules != null && afterRules.TryGetValue(property.Name, out var after))
                {
                    after(writer);
                }
            }
        }

        public static INetworkPacketWriter Serialize(object packet)
        {
            var writer = new NetworkPacketWriter();

            writer.WriteShort(GetPacketIdentifierFromAttribute(packet));

            if (InvokeOnSerializeIfExists(packet, writer))
            {
                return writer;
            }

            InvokeOnConfigureRules(packet);
            AddObjectToWriter(packet, writer);

            return writer;
        }

        private static void WriteStringListPropertyToWriter(List<string> list, NetworkPacketWriter writer)
        {
            writer.WriteInteger(list.Count);

            foreach (var s in list)
            {
                writer.WriteString(s ?? "");
            }
        }

        private static void WriteArbitraryListPropertyToWriter(PropertyInfo property, NetworkPacketWriter writer, object packet)
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

        private static void WriteProperty(PropertyInfo property, NetworkPacketWriter writer, object packet)
        {
            if (TryWritePrimitive(property, packet, writer))
            {
                return;
            }

            var type = property.PropertyType;
            var value = PropertyAccessorCache.GetValue(property, packet);

            if (type == typeof(List<string>))
            {
                WriteStringListPropertyToWriter((List<string>)value, writer);
                return;
            }

            if (type == typeof(Dictionary<int, string>))
            {
                WriteDictionary((Dictionary<int, string>)value, writer, writer.WriteInteger, v => writer.WriteString(v ?? ""));
                return;
            }

            if (type == typeof(Dictionary<long, string>))
            {
                WriteDictionary((Dictionary<long, string>)value, writer, writer.WriteLong, v => writer.WriteString(v ?? ""));
                return;
            }

            if (type == typeof(Dictionary<int, long>))
            {
                WriteDictionary((Dictionary<int, long>)value, writer, writer.WriteInteger, writer.WriteLong);
                return;
            }

            if (type == typeof(Dictionary<int, List<string>>))
            {
                var dict = (Dictionary<int, List<string>>)value;

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
                WriteDictionary((Dictionary<string, int>)value, writer, writer.WriteString, writer.WriteInteger);
                return;
            }

            if (type == typeof(Dictionary<string, string>))
            {
                WriteDictionary((Dictionary<string, string>)value, writer, writer.WriteString, writer.WriteString);
                return;
            }

            if (type.IsGenericType &&
                (type.GetGenericTypeDefinition() == typeof(List<>) ||
                 type.GetGenericTypeDefinition() == typeof(Collection<>)))
            {
                WriteArbitraryListPropertyToWriter(property, writer, packet);
                return;
            }

            AddObjectToWriter(value, writer, true);
        }

        private static void WriteType(Type type, object value, NetworkPacketWriter writer)
        {
            if (primitiveWriters.TryGetValue(type, out var action))
            {
                action(value, writer);
            }
        }
    }
}
