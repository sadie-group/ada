using System.Buffers.Binary;
using System.Text;
using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Packets;
using Ada.Networking.Packets;
using Ada.Networking.Packets.Serialization;

namespace Ada.Tests.Networking.Packets;

[TestFixture]
public class PacketFieldOrderTests
{
    private sealed class OrderedWriter : AbstractPacketWriter
    {
        public int First { get; init; }
        public string Second { get; init; } = "";
        public bool Third { get; init; }
        public int Fourth { get; init; }
    }

    private sealed class BaseWriter : AbstractPacketWriter
    {
        public int Alpha { get; init; }
        public int Beta { get; init; }
    }

    private sealed class FixedIdMap : IPacketIdMap
    {
        public bool TryGetHandlerType(short packetId, out Type? handlerType)
        {
            handlerType = null;
            return false;
        }

        public bool TryGetOutgoingId(Type writerType, out short packetId)
        {
            packetId = 7;
            return true;
        }
    }

    private sealed class TestCodec : IPacketCodec
    {
        public string Revision => "PRODUCTION";
        public IPacketIdMap IdMap { get; } = new FixedIdMap();
        public INetworkPacketDecoder Decoder { get; } = new NetworkPacketDecoder();
        public INetworkPacketWriter CreateWriter() => new NetworkPacketWriter();
        public INetworkPacketReader CreateReader(ReadOnlyMemory<byte> body) => new NetworkPacketReader(body);
    }

    private static byte[] Serialize(AbstractPacketWriter writer) =>
        NetworkPacketWriterSerializer.Serialize(writer, new TestCodec()).GetAllBytes();

    [Test]
    public void Fields_AreWrittenInDeclarationOrder()
    {
        var bytes = Serialize(new OrderedWriter
        {
            First = 0x11223344,
            Second = "ab",
            Third = true,
            Fourth = 0x55667788
        });

        var body = bytes[sizeof(int)..];

        var packetId = BinaryPrimitives.ReadInt16BigEndian(body);
        var first = BinaryPrimitives.ReadInt32BigEndian(body.AsSpan(2));
        var stringLength = BinaryPrimitives.ReadInt16BigEndian(body.AsSpan(6));
        var second = Encoding.UTF8.GetString(body, 8, 2);
        var third = body[10];
        var fourth = BinaryPrimitives.ReadInt32BigEndian(body.AsSpan(11));

        Assert.Multiple(() =>
        {
            Assert.That(packetId, Is.EqualTo(7), "packet id");
            Assert.That(first, Is.EqualTo(0x11223344));
            Assert.That(stringLength, Is.EqualTo(2));
            Assert.That(second, Is.EqualTo("ab"));
            Assert.That(third, Is.EqualTo(1), "bool");
            Assert.That(fourth, Is.EqualTo(0x55667788));
            Assert.That(body, Has.Length.EqualTo(15));
        });
    }

    [Test]
    public void Serialization_IsStableAcrossRepeatedCalls()
    {
        var first = Serialize(new BaseWriter { Alpha = 1, Beta = 2 });
        var second = Serialize(new BaseWriter { Alpha = 1, Beta = 2 });

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void DeclarationOrder_NotAlphabetical_IsRespected()
    {
        var bytes = Serialize(new BaseWriter { Alpha = 111, Beta = 222 });
        var body = bytes[sizeof(int)..];

        var alpha = BinaryPrimitives.ReadInt32BigEndian(body.AsSpan(2));
        var beta = BinaryPrimitives.ReadInt32BigEndian(body.AsSpan(6));

        Assert.Multiple(() =>
        {
            Assert.That(alpha, Is.EqualTo(111));
            Assert.That(beta, Is.EqualTo(222));
        });
    }
}
