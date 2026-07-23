using System.Numerics;
using System.Text;
using Ada.Networking.Encryption;
using Ada.Networking.Encryption.Extensions;
using Ada.Options.Options;

namespace Ada.Tests.Networking.Encryption;

[TestFixture]
public class HabboEncryptionTests
{
    private static HabboEncryption CreateEncryption()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new EncryptionOptions
        {
            Enabled = true,
            E = RsaCryptoTests.E,
            N = RsaCryptoTests.N,
            D = RsaCryptoTests.D,
        });

        return new HabboEncryption(options);
    }

    private static string DecryptAsClient(string hex)
    {
        var bytes = RsaClientEmulator.DecryptWithPublicKeyAndUnpadType1(hex.ToBytes(), RsaCryptoTests.E, RsaCryptoTests.N);
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
            .EncryptWithPublicKey(Encoding.Default.GetBytes(clientPublic.ToString()), RsaCryptoTests.E, RsaCryptoTests.N)
            .ToHexString();
        var serverShared = encryption.CalculateDiffieHellmanSharedKey(encryptedClientPublic);

        var expected = clientShared.ToByteArray();
        Array.Reverse(expected);

        Assert.That(serverShared, Is.EqualTo(expected));
    }
}
