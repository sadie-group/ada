using Ada.Networking;
using Ada.Networking.Packets;

namespace Ada.Tests.Networking;

[TestFixture]
public class EventSerializerTests
{
    private class SimpleHandler
    {
        public int Count { get; set; }
        public string Name { get; set; } = "";
        public bool Enabled { get; set; }
    }

    private class IntListHandler
    {
        public List<int> Ids { get; set; } = [];
    }

    private class StringListHandler
    {
        public List<string> Tags { get; set; } = [];
    }

    private class DictionaryHandler
    {
        public Dictionary<string, string> Values { get; set; } = [];
    }

    private class UnsupportedHandler
    {
        public double Value { get; set; }
    }

    private class BadgePart
    {
        public int PartId { get; set; }
        public int ColorId { get; set; }
        public int Position { get; set; }
    }

    private class RecordListHandler
    {
        public string Name { get; set; } = "";
        public int RoomId { get; set; }
        public List<BadgePart> Parts { get; set; } = [];
    }

    private static NetworkPacketReader ReaderFor(Action<NetworkPacketWriter> write)
    {
        var writer = new NetworkPacketWriter();
        write(writer);
        return new NetworkPacketReader(writer.GetAllBytes().Skip(4).ToArray());
    }

    [Test]
    public void SetProperties_ScalarProperties_Populated()
    {
        var handler = new SimpleHandler();

        EventSerializer.SetPropertiesForEventHandler(handler, ReaderFor(w =>
        {
            w.WriteInteger(7);
            w.WriteString("ada");
            w.WriteBool(true);
        }));

        Assert.Multiple(() =>
        {
            Assert.That(handler.Count, Is.EqualTo(7));
            Assert.That(handler.Name, Is.EqualTo("ada"));
            Assert.That(handler.Enabled, Is.True);
        });
    }

    [Test]
    public void SetProperties_IntListProperty_Populated()
    {
        var handler = new IntListHandler();

        EventSerializer.SetPropertiesForEventHandler(handler, ReaderFor(w =>
        {
            w.WriteInteger(2);
            w.WriteInteger(10);
            w.WriteInteger(20);
        }));

        Assert.That(handler.Ids, Is.EqualTo(new List<int> { 10, 20 }));
    }

    [Test]
    public void SetProperties_StringListProperty_Populated()
    {
        var handler = new StringListHandler();

        EventSerializer.SetPropertiesForEventHandler(handler, ReaderFor(w =>
        {
            w.WriteInteger(2);
            w.WriteString("a");
            w.WriteString("b");
        }));

        Assert.That(handler.Tags, Is.EqualTo(new List<string> { "a", "b" }));
    }

    [Test]
    public void SetProperties_DictionaryProperty_Populated()
    {
        var handler = new DictionaryHandler();

        EventSerializer.SetPropertiesForEventHandler(handler, ReaderFor(w =>
        {
            w.WriteInteger(4);
            w.WriteString("k1");
            w.WriteString("v1");
            w.WriteString("k2");
            w.WriteString("v2");
        }));

        Assert.That(handler.Values, Is.EqualTo(new Dictionary<string, string>
        {
            ["k1"] = "v1",
            ["k2"] = "v2"
        }));
    }

    [Test]
    public void SetProperties_ListOfRecords_FillsNestedTriplets()
    {
        var handler = new RecordListHandler();

        EventSerializer.SetPropertiesForEventHandler(handler, ReaderFor(w =>
        {
            w.WriteString("Alpha");
            w.WriteInteger(7);
            w.WriteInteger(2);
            w.WriteInteger(1); w.WriteInteger(2); w.WriteInteger(3);
            w.WriteInteger(4); w.WriteInteger(5); w.WriteInteger(6);
        }));

        Assert.Multiple(() =>
        {
            Assert.That(handler.Name, Is.EqualTo("Alpha"));
            Assert.That(handler.RoomId, Is.EqualTo(7));
            Assert.That(handler.Parts, Has.Count.EqualTo(2));
            Assert.That(handler.Parts[0].PartId, Is.EqualTo(1));
            Assert.That(handler.Parts[0].ColorId, Is.EqualTo(2));
            Assert.That(handler.Parts[0].Position, Is.EqualTo(3));
            Assert.That(handler.Parts[1].PartId, Is.EqualTo(4));
            Assert.That(handler.Parts[1].ColorId, Is.EqualTo(5));
            Assert.That(handler.Parts[1].Position, Is.EqualTo(6));
        });
    }

    [Test]
    public void SetProperties_EmptyRecordList_ReadsCountOnly()
    {
        var handler = new RecordListHandler();

        EventSerializer.SetPropertiesForEventHandler(handler, ReaderFor(w =>
        {
            w.WriteString("Beta");
            w.WriteInteger(3);
            w.WriteInteger(0);
        }));

        Assert.Multiple(() =>
        {
            Assert.That(handler.Name, Is.EqualTo("Beta"));
            Assert.That(handler.RoomId, Is.EqualTo(3));
            Assert.That(handler.Parts, Is.Empty);
        });
    }

    [Test]
    public void SetProperties_UnsupportedPropertyType_Throws()
    {
        var handler = new UnsupportedHandler();

        Assert.Throws<Exception>(() =>
            EventSerializer.SetPropertiesForEventHandler(handler, new NetworkPacketReader([])));
    }
}
