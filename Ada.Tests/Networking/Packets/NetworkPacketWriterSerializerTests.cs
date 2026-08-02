using System.Buffers.Binary;
using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Packets;
using Ada.Networking.Packets.Serialization;

namespace Ada.Tests.Networking.Packets;

[TestFixture]
public class NetworkPacketWriterSerializerTests
{
    [PacketId(42)]
    private class ScalarPacket : AbstractPacketWriter
    {
        public int Number { get; init; }
        public string Text { get; init; } = "";
        public bool Flag { get; init; }
    }

    [PacketId(43)]
    private class StringListPacket : AbstractPacketWriter
    {
        public List<string> Items { get; init; } = [];
    }

    [PacketId(44)]
    private class CustomSerializePacket : AbstractPacketWriter
    {
        public int Ignored { get; init; }

        public override void OnSerialize(INetworkPacketWriter writer)
        {
            writer.WriteInteger(99);
        }
    }

    [PacketId(45)]
    private class ConvertedPacket : AbstractPacketWriter
    {
        public int Number { get; init; }

        public override void OnConfigureRules()
        {
            Convert<string>(GetType().GetProperty(nameof(Number))!, value => $"n{value}");
        }
    }

    [PacketId(46)]
    private class OverriddenPacket : AbstractPacketWriter
    {
        public int Number { get; init; }

        public override void OnConfigureRules()
        {
            Override(GetType().GetProperty(nameof(Number))!, writer => writer.WriteInteger(-1));
        }
    }

    private class NoAttributePacket : AbstractPacketWriter;

    private static byte[] Payload(object packet)
    {
        var writer = (NetworkPacketWriter)NetworkPacketWriterSerializer.Serialize(packet, TestPacketCodec.Instance);
        return writer.GetAllBytes().Skip(4).ToArray();
    }

    private static short PacketIdOf(byte[] payload)
        => BinaryPrimitives.ReadInt16BigEndian(payload);

    [Test]
    public void Serialize_WritesPacketIdFromAttribute()
    {
        var payload = Payload(new ScalarPacket());
        Assert.That(PacketIdOf(payload), Is.EqualTo(42));
    }

    [Test]
    public void Serialize_MissingPacketIdAttribute_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => NetworkPacketWriterSerializer.Serialize(new NoAttributePacket(), TestPacketCodec.Instance));
    }

    [Test]
    public void Serialize_ScalarProperties_ReadBackWithReader()
    {
        var payload = Payload(new ScalarPacket { Number = 7, Text = "ada", Flag = true });

        var reader = new NetworkPacketReader(payload.AsMemory(2));
        var number = reader.ReadInt();
        var text = reader.ReadString();
        var flag = reader.ReadBool();

        Assert.Multiple(() =>
        {
            Assert.That(number, Is.EqualTo(7));
            Assert.That(text, Is.EqualTo("ada"));
            Assert.That(flag, Is.True);
        });
    }

    [Test]
    public void Serialize_StringList_CountThenItems()
    {
        var payload = Payload(new StringListPacket { Items = ["a", "b"] });

        var reader = new NetworkPacketReader(payload.AsMemory(2));
        var count = reader.ReadInt();
        var first = reader.ReadString();
        var second = reader.ReadString();

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(2));
            Assert.That(first, Is.EqualTo("a"));
            Assert.That(second, Is.EqualTo("b"));
        });
    }

    [Test]
    public void Serialize_OnSerializeOverride_BypassesReflection()
    {
        var payload = Payload(new CustomSerializePacket { Ignored = 5 });

        var reader = new NetworkPacketReader(payload.AsMemory(2));
        var value = reader.ReadInt();

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.EqualTo(99));
            Assert.That(payload, Has.Length.EqualTo(2 + 4), "only the custom int should follow the packet id");
        });
    }

    [Test]
    public void Serialize_ConversionRule_WritesConvertedType()
    {
        var payload = Payload(new ConvertedPacket { Number = 8 });

        var reader = new NetworkPacketReader(payload.AsMemory(2));
        Assert.That(reader.ReadString(), Is.EqualTo("n8"));
    }

    [Test]
    public void Serialize_OverrideRule_ReplacesPropertyValue()
    {
        var payload = Payload(new OverriddenPacket { Number = 8 });

        var reader = new NetworkPacketReader(payload.AsMemory(2));
        Assert.That(reader.ReadInt(), Is.EqualTo(-1));
    }
}
