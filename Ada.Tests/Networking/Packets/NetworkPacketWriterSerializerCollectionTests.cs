using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Packets;
using Ada.Networking.Packets.Serialization;

namespace Ada.Tests.Networking.Packets;

[TestFixture]
public class NetworkPacketWriterSerializerCollectionTests
{
    [PacketId(50)]
    private class IntStringDictionaryPacket : AbstractPacketWriter
    {
        public Dictionary<int, string> Values { get; init; } = [];
    }

    [PacketId(51)]
    private class StringIntDictionaryPacket : AbstractPacketWriter
    {
        public Dictionary<string, int> Values { get; init; } = [];
    }

    [PacketId(52)]
    private class StringStringDictionaryPacket : AbstractPacketWriter
    {
        public Dictionary<string, string> Values { get; init; } = [];
    }

    [PacketId(53)]
    private class IntLongDictionaryPacket : AbstractPacketWriter
    {
        public Dictionary<int, long> Values { get; init; } = [];
    }

    [PacketId(54)]
    private class NestedListDictionaryPacket : AbstractPacketWriter
    {
        public Dictionary<int, List<string>> Values { get; init; } = [];
    }

    private class BadgeSlot
    {
        public int Slot { get; init; }
        public string Code { get; init; } = "";
    }

    [PacketId(55)]
    private class RecordListPacket : AbstractPacketWriter
    {
        public List<BadgeSlot> Badges { get; init; } = [];
    }

    private static byte[] Payload(object packet)
    {
        var writer = (NetworkPacketWriter)NetworkPacketWriterSerializer.Serialize(packet, TestPacketCodec.Instance);
        return writer.GetAllBytes().Skip(4).ToArray();
    }

    [Test]
    public void Serialize_IntStringDictionary_CountThenPairs()
    {
        var payload = Payload(new IntStringDictionaryPacket { Values = { [3] = "c" } });

        var reader = new NetworkPacketReader(payload.AsMemory(2));
        var count = reader.ReadInt();
        var key = reader.ReadInt();
        var value = reader.ReadString();

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(key, Is.EqualTo(3));
            Assert.That(value, Is.EqualTo("c"));
        });
    }

    [Test]
    public void Serialize_StringIntDictionary_CountThenPairs()
    {
        var payload = Payload(new StringIntDictionaryPacket { Values = { ["hp"] = 100 } });

        var reader = new NetworkPacketReader(payload.AsMemory(2));
        var count = reader.ReadInt();
        var key = reader.ReadString();
        var value = reader.ReadInt();

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(key, Is.EqualTo("hp"));
            Assert.That(value, Is.EqualTo(100));
        });
    }

    [Test]
    public void Serialize_StringStringDictionary_CountThenPairs()
    {
        var payload = Payload(new StringStringDictionaryPacket { Values = { ["k"] = "v" } });

        var reader = new NetworkPacketReader(payload.AsMemory(2));
        var count = reader.ReadInt();
        var key = reader.ReadString();
        var value = reader.ReadString();

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(key, Is.EqualTo("k"));
            Assert.That(value, Is.EqualTo("v"));
        });
    }

    [Test]
    public void Serialize_IntLongDictionary_WritesLongAsFourBytes()
    {
        var payload = Payload(new IntLongDictionaryPacket { Values = { [1] = 42L } });

        var reader = new NetworkPacketReader(payload.AsMemory(2));
        var count = reader.ReadInt();
        var key = reader.ReadInt();
        var value = reader.ReadInt();

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(key, Is.EqualTo(1));
            Assert.That(value, Is.EqualTo(42), "WriteLong emits 4 bytes on the wire");
        });
    }

    [Test]
    public void Serialize_DictionaryOfStringLists_WritesKeyThenItemsWithoutInnerCount()
    {
        var payload = Payload(new NestedListDictionaryPacket { Values = { [9] = ["a", "b"] } });

        var reader = new NetworkPacketReader(payload.AsMemory(2));
        var count = reader.ReadInt();
        var key = reader.ReadInt();
        var first = reader.ReadString();
        var second = reader.ReadString();

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(key, Is.EqualTo(9));
            Assert.That(first, Is.EqualTo("a"));
            Assert.That(second, Is.EqualTo("b"));
        });
    }

    [Test]
    public void Serialize_ListOfRecords_CountThenEachRecordsProperties()
    {
        var payload = Payload(new RecordListPacket
        {
            Badges = [new BadgeSlot { Slot = 1, Code = "ADM" }, new BadgeSlot { Slot = 2, Code = "VIP" }],
        });

        var reader = new NetworkPacketReader(payload.AsMemory(2));
        var count = reader.ReadInt();
        var slot1 = reader.ReadInt();
        var code1 = reader.ReadString();
        var slot2 = reader.ReadInt();
        var code2 = reader.ReadString();

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(2));
            Assert.That((slot1, code1), Is.EqualTo((1, "ADM")));
            Assert.That((slot2, code2), Is.EqualTo((2, "VIP")));
        });
    }
}
