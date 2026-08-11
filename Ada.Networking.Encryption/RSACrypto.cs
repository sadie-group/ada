using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using Ada.Networking.Encryption.Extensions;

namespace Ada.Networking.Encryption;

public class RsaCrypto(string exponent, string modules, string privateExponent)
{
    private readonly BigInteger _exponent = BigInteger.Parse(exponent, NumberStyles.HexNumber);
    private readonly BigInteger _modules = BigInteger.Parse(modules, NumberStyles.HexNumber);
    private readonly BigInteger _privateExponent = BigInteger.Parse(privateExponent, NumberStyles.HexNumber);

    public int BlockSize => GetBlockSize();

    private int GetBlockSize()
    {
        return ((int)_modules.GetBitLength() + 7) / 8;
    }

    public byte[] Encrypt(byte[] src, bool isPrivate = false)
    {
        return DoEncrypt(src, isPrivate ? DoPrivate : DoPublic);
    }

    public byte[] Decrypt(byte[] src, bool isPrivate = false)
    {
        return DoDecrypt(src, isPrivate ? DoPrivate : DoPublic);
    }

    private BigInteger DoPublic(BigInteger src)
    {
        return BigInteger.ModPow(src, _exponent, _modules);
    }

    private BigInteger DoPrivate(BigInteger src)
    {
        return BigInteger.ModPow(src, _privateExponent, _modules);
    }

    private byte[] DoEncrypt(byte[] src, RsaCalculateDelegate method)
    {
        return Pkcs1Pad(src).PerformCalculation(method);
    }

    private byte[] DoDecrypt(byte[] src, RsaCalculateDelegate method)
    {
        var normalised = TrimEncodingPadding(src, GetBlockSize());

        if (normalised.Length > GetBlockSize())
        {
            throw new ArgumentException("Src is to long to encrypt.");
        }

        return Pkcs1Unpad(normalised.PerformCalculation(method));
    }

    private static byte[] TrimEncodingPadding(byte[] src, int blockSize)
        => src.Length == blockSize + 1 && src[0] == 0 ? src[1..] : src;

    private byte[] Pkcs1Pad(byte[] source)
    {
        var n = GetBlockSize();

        var maxMessageLength = n - 11;

        if (source.Length > maxMessageLength)
        {
            throw new ArgumentException(
                $"Message of {source.Length} byte(s) does not fit a {n}-byte RSA key, which carries at " +
                $"most {maxMessageLength} byte(s) with PKCS#1 v1.5 padding.",
                nameof(source));
        }

        var bytes = new byte[n];

        var i = source.Length - 1;

        while (i >= 0 && n > 11)
        {
            bytes[--n] = source[i--];
        }

        bytes[--n] = 0;

        while (n > 2)
        {
            bytes[--n] = 255;
        }

        bytes[--n] = 1;
        bytes[--n] = 0;

        return bytes;
    }

    private static byte[] Pkcs1Unpad(byte[] src)
    {
        if (src.Length >= 1 && src[0] == 2)
        {
            var temp = new byte[src.Length + 1];
            Array.Copy(src, 0, temp, 1, src.Length);
            src = temp;
        }

        if (src.Length < 2 || src[0] != 0 || src[1] != 2)
        {
            throw new CryptographicException("PKCS v1.5 Decode Error");
        }

        var startIndex = 2;
        do
        {
            if (src.Length <= startIndex)
            {
                throw new CryptographicException("PKCS v1.5 Decode Error");
            }
        }
        while (src[startIndex++] != 0);

        var bytes = new byte[src.Length - startIndex];
        Array.Copy(src, startIndex, bytes, 0, bytes.Length);

        return bytes;
    }
}
