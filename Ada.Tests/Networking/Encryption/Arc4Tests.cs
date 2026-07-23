using System.Text;
using Ada.Networking.Encryption;

namespace Ada.Tests.Networking.Encryption;

[TestFixture]
public class Arc4Tests
{
    [Test]
    public void Parse_KnownVector_MatchesRc4Keystream()
    {
        var arc4 = new Arc4(Encoding.ASCII.GetBytes("Key"));
        var data = Encoding.ASCII.GetBytes("Plaintext");

        arc4.Parse(data);

        Assert.That(Convert.ToHexString(data), Is.EqualTo("BBF316E8D940AF0AD3"));
    }

    [Test]
    public void Parse_EncryptThenDecryptWithSameKey_RoundTrips()
    {
        var key = new byte[] { 1, 2, 3, 4, 5 };
        var original = Encoding.UTF8.GetBytes("hello world");
        var data = (byte[])original.Clone();

        new Arc4(key).Parse(data);
        Assert.That(data, Is.Not.EqualTo(original));

        new Arc4(key).Parse(data);
        Assert.That(data, Is.EqualTo(original));
    }

    [Test]
    public void Parse_DifferentKeys_ProduceDifferentCiphertext()
    {
        var original = Encoding.UTF8.GetBytes("hello world");
        var a = (byte[])original.Clone();
        var b = (byte[])original.Clone();

        new Arc4([1, 2, 3]).Parse(a);
        new Arc4([4, 5, 6]).Parse(b);

        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void Parse_StreamIsStateful_AcrossCalls()
    {
        var key = new byte[] { 9, 9, 9 };
        var full = new byte[] { 10, 20, 30, 40 };
        var first = new byte[] { 10, 20 };
        var second = new byte[] { 30, 40 };

        new Arc4(key).Parse(full);

        var arc4 = new Arc4(key);
        arc4.Parse(first);
        arc4.Parse(second);

        Assert.That(first.Concat(second), Is.EqualTo(full));
    }

    [Test]
    public void Initialize_ResetsKeystream()
    {
        var key = new byte[] { 7, 7, 7 };
        var arc4 = new Arc4(key);
        var a = new byte[] { 1, 2, 3 };
        var b = new byte[] { 1, 2, 3 };

        arc4.Parse(a);
        arc4.Initialize(key);
        arc4.Parse(b);

        Assert.That(b, Is.EqualTo(a));
    }
}
