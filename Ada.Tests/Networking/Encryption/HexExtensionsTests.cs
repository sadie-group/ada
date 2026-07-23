using Ada.Networking.Encryption.Extensions;

namespace Ada.Tests.Networking.Encryption;

[TestFixture]
public class HexExtensionsTests
{
    [Test]
    public void ToHexString_Bytes_UppercaseHex()
    {
        var result = new byte[] { 0x00, 0xAB, 0xFF }.ToHexString();
        Assert.That(result, Is.EqualTo("00ABFF"));
    }

    [Test]
    public void ToBytes_UppercaseHex_Bytes()
    {
        var result = "00ABFF".ToBytes();
        Assert.That(result, Is.EqualTo(new byte[] { 0x00, 0xAB, 0xFF }));
    }

    [Test]
    public void ToBytes_LowercaseHex_Bytes()
    {
        var result = "deadbeef".ToBytes();
        Assert.That(result, Is.EqualTo(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }));
    }

    [Test]
    public void ToBytes_RoundTripsWithToHexString()
    {
        var original = new byte[] { 1, 2, 3, 250, 251, 252 };
        Assert.That(original.ToHexString().ToBytes(), Is.EqualTo(original));
    }

    [Test]
    public void ToBytes_OddLength_Throws()
    {
        Assert.Throws<ArgumentException>(() => "ABC".ToBytes());
    }

    [Test]
    public void ToBytes_InvalidCharacter_Throws()
    {
        Assert.Throws<ArgumentException>(() => "ZZ".ToBytes());
    }
}
