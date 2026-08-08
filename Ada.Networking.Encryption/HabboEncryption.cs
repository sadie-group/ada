using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Ada.Networking.Encryption.Extensions;
using Ada.Options.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ada.Networking.Encryption;

public class HabboEncryption
{
    private const string _publishedModulus =
        "0086851DD364D5C5CECE3C883171CC6DDC5760779B992482BD1E20DD296888DF91B33B936A7B93F06D29E8870F70" +
        "3A216257DEC7C81DE0058FEA4CC5116F75E6EFC4E9113513E45357DC3FD43D4EFAB5963EF178B78BD61E81A14C60" +
        "3B24C8BCCE0A12230B320045498EDC29282FF0603BC7B7DAE8FC1B05B52B2F301A9DC783B7";

    private readonly RsaCrypto _crypto;
    private readonly DiffieHellman _diffieHellman = new();

    public HabboEncryption(IOptions<EncryptionOptions> options, ILogger<HabboEncryption> logger)
    {
        _crypto = new RsaCrypto(options.Value.E, options.Value.N, options.Value.D);

        if (string.Equals(options.Value.N.Trim(), _publishedModulus, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Encryption:N is the published example RSA key, whose private exponent is public. The " +
                "Diffie-Hellman handshake it signs provides no authentication. Generate your own " +
                "keypair (and rely on NetworkOptions:UseWss for confidentiality).");
        }
    }

    private string GetRsaEncryptedString(string message)
    {
        var bytes = Encoding.Default.GetBytes(message);
        var encryptedBytes = _crypto.Encrypt(bytes, true);

        return encryptedBytes.ToHexString();
    }

    private string GetRsaDecryptedString(string data)
    {
        var bytes = data.ToBytes();
        var decryptedBytes = _crypto.Decrypt(bytes, true);

        return Encoding.Default.GetString(decryptedBytes);
    }

    public string GetRsaDiffieHellmanPrimeKey()
    {
        var key = _diffieHellman.Prime.ToString();
        return GetRsaEncryptedString(key);
    }

    public string GetRsaDiffieHellmanGeneratorKey()
    {
        var key = _diffieHellman.Generator.ToString();
        return GetRsaEncryptedString(key);
    }

    public string GetRsaDiffieHellmanPublicKey()
    {
        var key = _diffieHellman.PublicKey.ToString();
        return GetRsaEncryptedString(key);
    }

    public bool TryCalculateDiffieHellmanSharedKey(string publicKey, [NotNullWhen(true)] out byte[]? sharedKey)
    {
        sharedKey = null;

        string decrypted;

        try
        {
            decrypted = GetRsaDecryptedString(publicKey);
        }
        catch (Exception e) when (e is CryptographicException or ArgumentException or IndexOutOfRangeException)
        {
            return false;
        }

        if (!BigInteger.TryParse(decrypted, out var peerKey) ||
            !_diffieHellman.TryCalculateSharedKey(peerKey, out var shared))
        {
            return false;
        }

        var result = shared.ToByteArray();
        Array.Reverse(result);

        sharedKey = result;
        return true;
    }
}
