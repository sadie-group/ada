using System.Text;
using Ada.Networking.Packets;

namespace Ada.Tests.Networking.Packets;

[TestFixture]
public class PacketParsingFuzzTests
{
    private const int _iterations = 20_000;

    private static NetworkPacketDecoder Decoder => new();

    private static Random NewRandom() => new(20260809);

    [Test]
    public void Decode_ArbitraryBytes_ThrowsOnlyMalformedPacketException()
    {
        var random = NewRandom();
        var decoder = Decoder;

        for (var i = 0; i < _iterations; i++)
        {
            var buffer = new byte[random.Next(0, 64)];
            random.NextBytes(buffer);

            try
            {
                decoder.Decode(Guid.NewGuid(), buffer, buffer.Length);
            }
            catch (MalformedPacketException)
            {
            }
            catch (Exception e)
            {
                Assert.Fail(
                    $"Decoding {buffer.Length} arbitrary byte(s) threw {e.GetType().Name}: {e.Message}. " +
                    "Only MalformedPacketException is survivable by the connection loop.");
            }
        }
    }

    [Test]
    public void Decode_TruncatedFrames_NeverThrowsOutOfRange()
    {
        var full = new byte[] { 0, 0, 0, 12, 0, 100, 0, 3, 65, 66, 67, 0, 0, 0, 9 };
        var decoder = Decoder;

        for (var length = 0; length <= full.Length; length++)
        {
            try
            {
                decoder.Decode(Guid.NewGuid(), full, length);
            }
            catch (MalformedPacketException)
            {
            }
            catch (Exception e)
            {
                Assert.Fail($"Truncating to {length} byte(s) threw {e.GetType().Name}.");
            }
        }
    }

    [Test]
    public void Reader_ArbitraryBodies_ThrowOnlyMalformedPacketException()
    {
        var random = NewRandom();

        for (var i = 0; i < _iterations; i++)
        {
            var body = new byte[random.Next(0, 48)];
            random.NextBytes(body);

            var reader = new NetworkPacketReader(body);

            try
            {
                switch (i % 5)
                {
                    case 0:
                        reader.ReadString();
                        break;
                    case 1:
                        reader.ReadInt();
                        reader.ReadString();
                        break;
                    case 2:
                        reader.ReadShort();
                        reader.ReadShort();
                        reader.ReadString();
                        break;
                    case 3:
                        reader.ReadBool();
                        reader.ReadInt();
                        reader.ReadInt();
                        break;
                    default:
                        while (reader.Remaining > 0)
                        {
                            reader.ReadByte();
                        }

                        reader.ReadInt();
                        break;
                }
            }
            catch (MalformedPacketException)
            {
            }
            catch (Exception e)
            {
                Assert.Fail(
                    $"Reading a {body.Length}-byte body threw {e.GetType().Name}: {e.Message}.");
            }
        }
    }

    [Test]
    public void Reader_NegativeStringLength_IsRejectedNotTrusted()
    {
        var body = new byte[] { 0xFF, 0xFF, 1, 2, 3, 4 };
        var reader = new NetworkPacketReader(body);

        Assert.Throws<MalformedPacketException>(() => reader.ReadString());
    }

    [Test]
    public void Reader_StringLongerThanBody_IsRejected()
    {
        var body = new byte[] { 0, 200, 65, 66 };
        var reader = new NetworkPacketReader(body);

        Assert.Throws<MalformedPacketException>(() => reader.ReadString());
    }

    [Test]
    public void Reader_WellFormedString_StillRoundTrips()
    {
        var text = "hello";
        var body = new byte[2 + text.Length];
        body[0] = 0;
        body[1] = (byte) text.Length;
        Encoding.UTF8.GetBytes(text).CopyTo(body, 2);

        var reader = new NetworkPacketReader(body);

        Assert.That(reader.ReadString(), Is.EqualTo(text));
    }
}
