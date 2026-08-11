using System.Collections;
using System.Reflection;
using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Packets;
using Ada.Networking.Packets;
using Ada.Networking.Packets.Serialization;
using Ada.Networking.Writers.Generated;

namespace Ada.Tests.Networking.Packets;

[TestFixture]
public class GeneratedPacketWriterParityTests
{
    private sealed class FixedIdMap : IPacketIdMap
    {
        public bool TryGetHandlerType(short packetId, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Type? handlerType)
        {
            handlerType = null;
            return false;
        }

        public bool TryGetOutgoingId(Type writerType, out short packetId)
        {
            packetId = 1;
            return true;
        }
    }

    private sealed class TestCodec : IPacketCodec
    {
        public string Revision => "TEST";
        public IPacketIdMap IdMap { get; } = new FixedIdMap();
        public INetworkPacketDecoder Decoder => throw new NotSupportedException();
        public INetworkPacketWriter CreateWriter() => new NetworkPacketWriter();
        public INetworkPacketReader CreateReader(ReadOnlyMemory<byte> body) => new NetworkPacketReader(body);
    }

    [TearDown]
    public void TearDown() => PacketWriterFastPath.Handler = GeneratedPacketWriters.TrySerialize;

    [Test]
    public void GeneratedSerialization_IsByteIdentical_ToReflection()
    {
        var codec = new TestCodec();

        var writerTypes = typeof(GeneratedPacketWriters).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && typeof(AbstractPacketWriter).IsAssignableFrom(t)
                        && t.GetConstructor(Type.EmptyTypes) is not null
                        && GeneratedPacketWriters.Handles(t))
            .OrderBy(t => t.FullName, StringComparer.Ordinal);

        var covered = 0;
        var mismatches = new List<string>();

        foreach (var type in writerTypes)
        {
            var reflectionTarget = (AbstractPacketWriter) Activator.CreateInstance(type)!;
            var generatedTarget = (AbstractPacketWriter) Activator.CreateInstance(type)!;
            Populate(reflectionTarget, 0);
            Populate(generatedTarget, 0);

            byte[] reflected;

            try
            {
                PacketWriterFastPath.Handler = null;
                reflected = NetworkPacketWriterSerializer.Serialize(reflectionTarget, codec).GetAllBytes();
            }
            catch
            {
                continue;
            }

            PacketWriterFastPath.Handler = GeneratedPacketWriters.TrySerialize;
            var generated = NetworkPacketWriterSerializer.Serialize(generatedTarget, codec).GetAllBytes();

            covered++;

            if (!reflected.AsSpan().SequenceEqual(generated))
            {
                mismatches.Add(type.FullName!);
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(covered, Is.GreaterThan(80), "expected the generator to cover the bulk of reflection writers");
            Assert.That(mismatches, Is.Empty, "byte-parity mismatches: " + string.Join(", ", mismatches));
        });
    }

    [Test]
    public void WriteFramedTo_IsByteIdentical_ToGetAllBytes()
    {
        var writer = new NetworkPacketWriter();
        writer.WriteShort(3);
        writer.WriteInteger(42);
        writer.WriteString("hello world");
        writer.WriteLong(7);
        writer.WriteBool(true);
        writer.WriteByte(9);

        var expected = writer.GetAllBytes();
        var framed = new byte[writer.FramedLength];
        writer.WriteFramedTo(framed);

        Assert.That(framed, Is.EqualTo(expected));
    }

    private static void Populate(object target, int depth)
    {
        foreach (var property in target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0 || property.SetMethod is null)
            {
                continue;
            }

            if (property.DeclaringType == typeof(AbstractPacketWriter))
            {
                continue;
            }

            try
            {
                var value = CreateValue(property.PropertyType, depth);

                if (value is not null)
                {
                    property.SetValue(target, value);
                }
            }
            catch
            {
            }
        }
    }

    private static object? CreateValue(Type type, int depth)
    {
        if (type == typeof(string))
        {
            return "x";
        }

        if (type == typeof(int))
        {
            return 7;
        }

        if (type == typeof(short))
        {
            return (short) 3;
        }

        if (type == typeof(long))
        {
            return 4L;
        }

        if (type == typeof(bool))
        {
            return true;
        }

        if (type == typeof(byte))
        {
            return (byte) 9;
        }

        if (type.IsEnum || depth >= 3)
        {
            return null;
        }

        if (type.IsArray)
        {
            var element = type.GetElementType()!;
            var value = CreateValue(element, depth + 1);

            if (value is null)
            {
                return Array.CreateInstance(element, 0);
            }

            var array = Array.CreateInstance(element, 1);
            array.SetValue(value, 0);
            return array;
        }

        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            var arguments = type.GetGenericArguments();

            if (definition == typeof(List<>) || definition == typeof(IList<>) || definition == typeof(ICollection<>)
                || definition == typeof(IReadOnlyList<>) || definition == typeof(IReadOnlyCollection<>) || definition == typeof(HashSet<>))
            {
                var list = (IList) Activator.CreateInstance(typeof(List<>).MakeGenericType(arguments[0]))!;
                var element = CreateValue(arguments[0], depth + 1);

                if (element is not null)
                {
                    list.Add(element);
                }

                return list;
            }

            if (definition == typeof(Dictionary<,>))
            {
                var dictionary = (IDictionary) Activator.CreateInstance(type)!;
                var key = CreateValue(arguments[0], depth + 1);
                var value = CreateValue(arguments[1], depth + 1);

                if (key is not null && value is not null)
                {
                    dictionary.Add(key, value);
                }

                return dictionary;
            }

            return null;
        }

        if (type is { IsClass: true } && type.GetConstructor(Type.EmptyTypes) is not null)
        {
            var instance = Activator.CreateInstance(type)!;
            Populate(instance, depth + 1);
            return instance;
        }

        return null;
    }
}
