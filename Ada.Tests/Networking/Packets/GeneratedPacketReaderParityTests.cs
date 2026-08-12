using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Networking;
using Ada.Networking.Events.Generated;
using Ada.Networking.Packets;

namespace Ada.Tests.Networking.Packets;

[TestFixture]
public class GeneratedPacketReaderParityTests
{
    private int _seed;

    [TearDown]
    public void TearDown() => EventSerializer.FastFill = GeneratedPacketReaders.TryFill;

    [Test]
    public void GeneratedDeserialization_IsIdentical_ToReflection()
    {
        var handlerTypes = typeof(GeneratedPacketReaders).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
                        && typeof(INetworkPacketEventHandler).IsAssignableFrom(t))
            .OrderBy(t => t.FullName, StringComparer.Ordinal);

        var covered = 0;
        var mismatches = new List<string>();

        foreach (var type in handlerTypes)
        {
            _seed = 1;
            var body = BuildBody(type);

            if (body is null)
            {
                continue;
            }

            var generated = RuntimeHelpers.GetUninitializedObject(type);
            EventSerializer.FastFill = null;

            if (!GeneratedPacketReaders.TryFill(generated, new NetworkPacketReader(body)))
            {
                continue;
            }

            var reflected = RuntimeHelpers.GetUninitializedObject(type);
            EventSerializer.SetPropertiesForEventHandler(reflected, new NetworkPacketReader(body));

            covered++;

            foreach (var property in WritableProperties(type))
            {
                if (!DeepEqual(property.GetValue(generated), property.GetValue(reflected)))
                {
                    mismatches.Add(type.FullName + "." + property.Name);
                }
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(covered, Is.GreaterThan(100), "expected the generator to cover the bulk of data handlers");
            Assert.That(mismatches, Is.Empty, "value mismatches: " + string.Join(", ", mismatches));
        });
    }

    private byte[]? BuildBody(Type type)
    {
        var writer = new NetworkPacketWriter();

        foreach (var property in WritableProperties(type))
        {
            if (!WriteSample(writer, property.PropertyType))
            {
                return null;
            }
        }

        return writer.GetAllBytes().AsSpan(4).ToArray();
    }

    private bool WriteSample(NetworkPacketWriter writer, Type type)
    {
        if (type == typeof(int))
        {
            writer.WriteInteger(_seed++);
            return true;
        }

        if (type == typeof(long))
        {
            writer.WriteInteger(_seed++);
            return true;
        }

        if (type == typeof(string))
        {
            writer.WriteString("s" + _seed++);
            return true;
        }

        if (type == typeof(bool))
        {
            writer.WriteBool(_seed++ % 2 == 0);
            return true;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            var element = type.GetGenericArguments()[0];

            writer.WriteInteger(2);

            return WriteSample(writer, element) && WriteSample(writer, element);
        }

        if (type == typeof(Dictionary<string, string>))
        {
            writer.WriteInteger(4);

            for (var i = 0; i < 4; i++)
            {
                writer.WriteString("s" + _seed++);
            }

            return true;
        }

        if (type.IsClass && type.GetConstructor(Type.EmptyTypes) is not null)
        {
            foreach (var property in WritableProperties(type))
            {
                if (!WriteSample(writer, property.PropertyType))
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }

    private static IEnumerable<PropertyInfo> WritableProperties(Type type)
        => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && p.GetIndexParameters().Length == 0)
            .OrderBy(p => p.MetadataToken);

    private static bool DeepEqual(object? a, object? b)
    {
        if (a is null || b is null)
        {
            return a is null && b is null;
        }

        var type = a.GetType();

        if (type != b.GetType())
        {
            return false;
        }

        if (type.IsPrimitive || a is string)
        {
            return a.Equals(b);
        }

        if (a is IEnumerable enumerableA)
        {
            var listA = enumerableA.Cast<object>().ToList();
            var listB = ((IEnumerable) b).Cast<object>().ToList();

            return listA.Count == listB.Count && listA.Zip(listB, DeepEqual).All(equal => equal);
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(p => p.CanRead && p.GetIndexParameters().Length == 0))
        {
            if (!DeepEqual(property.GetValue(a), property.GetValue(b)))
            {
                return false;
            }
        }

        return true;
    }
}
