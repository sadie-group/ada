using System.Numerics;
using System.Text;
using Ada.Networking.Encryption;
using Ada.Networking.Encryption.Extensions;
using Ada.Options.Options;

namespace Ada.Tests.Networking.Encryption;

[TestFixture]
public class HabboEncryptionTests
{
    private const string _e = "10001";
    private const string _n = "0a0f79e1dd5d47a4c86389890cc0908cb91243cf72dc95158e8aa0f7886632a05f2733cf2566056710b97c1a9e6734e79b48c972623b7b52e1a59642cb4afea849a3aed9b78c4c16bb36036137bc22c3e94d2ba0732428de86290b5a3a32951f7dca991960984cd167020a531ddb98da79b7908550475d2b50713f2f987130d27";
    private const string _d = "0b4e38f9c979f9f903f857a6dbddca9359a75cecd3776f523a4d2f76fca15c633c8b217db7d95e58e4428d649c7dfec6078b5456666dd1b8e23ae3114cc38a4a175dc191db56aac8db0377b4557730cacd2fb9c56e1fe30d69cca8d12d63f255254b4811574f8a07a9130bea9738d80403aa819ccbacf074877041c052277641";

    private static HabboEncryption CreateEncryption()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new EncryptionOptions
        {
            Enabled = true,
            E = _e,
            N = _n,
            D = _d,
        });

        return new HabboEncryption(options, NullLogger<HabboEncryption>.Instance);
    }

    private static string DecryptAsClient(string hex)
    {
        var bytes = RsaClientEmulator.DecryptWithPublicKeyAndUnpadType1(hex.ToBytes(), _e, _n);
        return Encoding.Default.GetString(bytes);
    }

    [Test]
    public void GetRsaDiffieHellmanPrimeKey_DecryptsToBigInteger()
    {
        var encryption = CreateEncryption();

        var decrypted = DecryptAsClient(encryption.GetRsaDiffieHellmanPrimeKey());

        Assert.That(BigInteger.TryParse(decrypted, out _), Is.True);
    }

    [Test]
    public void GetRsaDiffieHellmanGeneratorKey_DecryptsToBigInteger()
    {
        var encryption = CreateEncryption();

        var decrypted = DecryptAsClient(encryption.GetRsaDiffieHellmanGeneratorKey());

        Assert.That(BigInteger.TryParse(decrypted, out _), Is.True);
    }

    [Test]
    public void CalculateDiffieHellmanSharedKey_AgreesWithClientSideCalculation()
    {
        var encryption = CreateEncryption();

        var serverPrime = BigInteger.Parse(DecryptAsClient(encryption.GetRsaDiffieHellmanPrimeKey()));
        var serverGenerator = BigInteger.Parse(DecryptAsClient(encryption.GetRsaDiffieHellmanGeneratorKey()));
        var serverPublic = BigInteger.Parse(DecryptAsClient(encryption.GetRsaDiffieHellmanPublicKey()));

        var clientPrivate = new BigInteger(982451653);
        var clientPublic = BigInteger.ModPow(serverGenerator, clientPrivate, serverPrime);
        var clientShared = BigInteger.ModPow(serverPublic, clientPrivate, serverPrime);

        var encryptedClientPublic = RsaClientEmulator
            .EncryptWithPublicKey(Encoding.Default.GetBytes(clientPublic.ToString()), _e, _n)
            .ToHexString();
        Assert.That(encryption.TryCalculateDiffieHellmanSharedKey(encryptedClientPublic, out var serverShared), Is.True);

        var expected = clientShared.ToByteArray();
        Array.Reverse(expected);

        Assert.That(serverShared, Is.EqualTo(expected));
    }

    [Test]
    public void TryCalculateDiffieHellmanSharedKey_Garbage_ReturnsFalse()
    {
        var encryption = CreateEncryption();

        Assert.That(encryption.TryCalculateDiffieHellmanSharedKey("00ff00ff", out _), Is.False);
    }
}
