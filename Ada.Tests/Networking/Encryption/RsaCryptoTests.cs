using System.Text;
using Ada.Networking.Encryption;

namespace Ada.Tests.Networking.Encryption;

[TestFixture]
public class RsaCryptoTests
{
    public const string E = "10001";
    public const string N = "066aaed762c154ccaaae558326a392461207e0c978b37148a9c3c5db78305b0aaf231fc3ba1802f30dbb681b34ffdca36c378d6c2100eaed613d2b18b3f144425";
    public const string D = "0092544cdf81355a9e01b5a05f3ac6f6f2f83caff5ce9652d0bc9dcd7521699ac23a21c3bcb33c9b13f4b5fb6c63baa9eb9a5c6fc6e7314734068b39eefd9a641";

    private static RsaCrypto CreateCrypto() => new(E, N, D);

    [Test]
    public void Decrypt_ClientEncryptedBlock_ReturnsMessage()
    {
        var crypto = CreateCrypto();
        var message = Encoding.UTF8.GetBytes("secret message");

        var cipher = RsaClientEmulator.EncryptWithPublicKey(message, E, N);
        var decrypted = crypto.Decrypt(cipher, true);

        Assert.That(decrypted, Is.EqualTo(message));
    }

    [Test]
    public void Encrypt_WithPrivateKey_ClientCanDecryptWithPublicKey()
    {
        var crypto = CreateCrypto();
        var message = Encoding.UTF8.GetBytes("signed payload");

        var cipher = crypto.Encrypt(message, true);
        var decrypted = RsaClientEmulator.DecryptWithPublicKeyAndUnpadType1(cipher, E, N);

        Assert.That(decrypted, Is.EqualTo(message));
    }

    [Test]
    public void Encrypt_ProducesDifferentBytesThanInput()
    {
        var crypto = CreateCrypto();
        var message = Encoding.UTF8.GetBytes("secret message");

        var encrypted = crypto.Encrypt(message, true);

        Assert.That(encrypted, Is.Not.EqualTo(message));
    }

    [Test]
    public void Encrypt_MessageLongerThanTheKeyCanCarry_ThrowsRatherThanTruncating()
    {
        var crypto = new RsaCrypto(E, N, D);

        var tooLong = Encoding.Default.GetBytes(new string('9', 54));

        Assert.Throws<ArgumentException>(() => crypto.Encrypt(tooLong, true));
    }

    [Test]
    public void Encrypt_MessageExactlyAtTheLimit_IsAccepted()
    {
        var crypto = new RsaCrypto(E, N, D);
        var atLimit = Encoding.Default.GetBytes(new string('9', 53));

        var roundTripped = Encoding.Default.GetString(
            RsaClientEmulator.DecryptWithPublicKeyAndUnpadType1(crypto.Encrypt(atLimit, true), E, N));

        Assert.That(roundTripped, Is.EqualTo(new string('9', 53)),
            "a message that fits must survive the round trip intact");
    }

    [Test]
    public void Decrypt_CiphertextWithLeadingZeroPadding_IsAccepted()
    {
        var crypto = new RsaCrypto(E, N, D);

        var cipher = RsaClientEmulator.EncryptWithPublicKey(
            Encoding.Default.GetBytes("round trip"), E, N);

        var widened = new byte[cipher.Length + 1];
        cipher.CopyTo(widened, 1);

        Assert.That(Encoding.Default.GetString(crypto.Decrypt(widened, true)), Is.EqualTo("round trip"),
            "a leading zero is an encoding artefact, not extra data");
    }

    [Test]
    public void Decrypt_InputLongerThanBlockSize_Throws()
    {
        var crypto = CreateCrypto();
        var tooLong = new byte[128];

        Assert.Throws<ArgumentException>(() => crypto.Decrypt(tooLong, true));
    }
}
