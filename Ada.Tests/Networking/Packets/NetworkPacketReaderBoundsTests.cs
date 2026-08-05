using Ada.Networking;
using Ada.Networking.Packets;

namespace Ada.Tests.Networking.Packets;

[TestFixture]
public class NetworkPacketReaderBoundsTests
{
    [Test]
    public void ReadInt_BodyTooShort_ThrowsMalformedPacket()
    {
        var reader = new NetworkPacketReader(new byte[] { 0x00, 0x01 });

        Assert.Throws<MalformedPacketException>(() => reader.ReadInt());
    }

    [Test]
    public void ReadString_LengthExceedsBody_ThrowsMalformedPacket()
    {
        var reader = new NetworkPacketReader(new byte[] { 0x01, 0x2C });

        Assert.Throws<MalformedPacketException>(() => reader.ReadString());
    }

    [Test]
    public void ReadString_NegativeLength_ThrowsMalformedPacket()
    {
        var reader = new NetworkPacketReader(new byte[] { 0xFF, 0xFF, 0x41, 0x42 });

        Assert.Throws<MalformedPacketException>(() => reader.ReadString());
    }

    [Test]
    public void ReadByte_EmptyBody_ThrowsMalformedPacket()
    {
        var reader = new NetworkPacketReader(Array.Empty<byte>());

        Assert.Throws<MalformedPacketException>(() => reader.ReadByte());
    }

    [Test]
    public void Remaining_TracksConsumedBytes()
    {
        var reader = new NetworkPacketReader(new byte[] { 0x00, 0x00, 0x00, 0x07, 0x01 });

        Assert.That(reader.Remaining, Is.EqualTo(5));
        Assert.That(reader.ReadInt(), Is.EqualTo(7));
        Assert.That(reader.Remaining, Is.EqualTo(1));
    }

    [Test]
    public void ReadString_ExactlyFitsBody_Succeeds()
    {
        var reader = new NetworkPacketReader(new byte[] { 0x00, 0x02, 0x68, 0x69 });

        Assert.That(reader.ReadString(), Is.EqualTo("hi"));
        Assert.That(reader.Remaining, Is.Zero);
    }

    private class ListHandler
    {
        public List<int> Ids { get; set; } = [];
    }

    [Test]
    public void FillProperties_ListCountExceedsBody_ThrowsInsteadOfPreallocating()
    {
        var reader = new NetworkPacketReader(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF });

        Assert.Throws<MalformedPacketException>(
            () => EventSerializer.SetPropertiesForEventHandler(new ListHandler(), reader));
    }

    [Test]
    public void FillProperties_NegativeListCount_Throws()
    {
        var reader = new NetworkPacketReader(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });

        Assert.Throws<MalformedPacketException>(
            () => EventSerializer.SetPropertiesForEventHandler(new ListHandler(), reader));
    }
}
