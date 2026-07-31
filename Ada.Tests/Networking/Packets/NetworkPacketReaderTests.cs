using System.Buffers.Binary;
using System.Text;
using Ada.Networking.Packets;

namespace Ada.Tests.Networking.Packets;

[TestFixture]
public class NetworkPacketReaderTests
{
    [Test]
    public void ReadInt_BigEndianBytes_Value()
    {
        var reader = new NetworkPacketReader("\0\0\0*"u8.ToArray());
        Assert.That(reader.ReadInt(), Is.EqualTo(42));
    }

    [Test]
    public void ReadInt_NegativeValue_RoundTrips()
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, -5);

        var reader = new NetworkPacketReader(bytes);
        Assert.That(reader.ReadInt(), Is.EqualTo(-5));
    }

    [Test]
    public void ReadLong_BigEndianBytes_Value()
    {
        var bytes = new byte[8];
        BinaryPrimitives.WriteInt64BigEndian(bytes, 1234567890123L);

        var reader = new NetworkPacketReader(bytes);
        Assert.That(reader.ReadLong(), Is.EqualTo(1234567890123L));
    }

    [Test]
    public void ReadBool_OneAndZero_TrueAndFalse()
    {
        var reader = new NetworkPacketReader(new byte[] { 1, 0 });
        Assert.That(reader.ReadBool(), Is.True);
        Assert.That(reader.ReadBool(), Is.False);
    }

    [Test]
    public void ReadString_LengthPrefixedUtf8_Value()
    {
        var text = Encoding.UTF8.GetBytes("héllo");
        var bytes = new byte[2 + text.Length];
        BinaryPrimitives.WriteInt16BigEndian(bytes, (short)text.Length);
        text.CopyTo(bytes, 2);

        var reader = new NetworkPacketReader(bytes);
        Assert.That(reader.ReadString(), Is.EqualTo("héllo"));
    }

    [Test]
    public void Read_SequentialValues_AdvancesPosition()
    {
        var reader = new NetworkPacketReader(new byte[] { 0, 0, 0, 1, 0, 0, 0, 2, 1 });

        Assert.That(reader.ReadInt(), Is.EqualTo(1));
        Assert.That(reader.ReadInt(), Is.EqualTo(2));
        Assert.That(reader.ReadBool(), Is.True);
    }
}
