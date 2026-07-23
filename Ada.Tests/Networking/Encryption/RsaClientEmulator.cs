using System.Globalization;
using System.Numerics;

namespace Ada.Tests.Networking.Encryption;

public static class RsaClientEmulator
{
    public static byte[] EncryptWithPublicKey(byte[] message, string eHex, string nHex)
    {
        var e = BigInteger.Parse(eHex, NumberStyles.HexNumber);
        var n = BigInteger.Parse(nHex, NumberStyles.HexNumber);
        var blockSize = ((int)n.GetBitLength() + 7) / 8;

        var padded = new byte[blockSize];
        padded[0] = 0;
        padded[1] = 2;
        for (var i = 2; i < blockSize - message.Length - 1; i++)
        {
            padded[i] = 0xAA;
        }

        message.CopyTo(padded, blockSize - message.Length);

        var cipher = BigInteger.ModPow(ToBigInteger(padded), e, n);
        return ToBigEndianBytes(cipher);
    }

    public static byte[] DecryptWithPublicKeyAndUnpadType1(byte[] cipher, string eHex, string nHex)
    {
        var e = BigInteger.Parse(eHex, NumberStyles.HexNumber);
        var n = BigInteger.Parse(nHex, NumberStyles.HexNumber);

        var padded = ToBigEndianBytes(BigInteger.ModPow(ToBigInteger(cipher), e, n));

        Assert.That(padded[0], Is.EqualTo(1), "expected PKCS#1 type 1 block");
        var index = 1;
        while (padded[index] == 0xFF)
        {
            index++;
        }

        Assert.That(padded[index], Is.EqualTo(0));
        return padded[(index + 1)..];
    }

    private static BigInteger ToBigInteger(byte[] bigEndian)
    {
        var reversed = (byte[])bigEndian.Clone();
        Array.Reverse(reversed);
        return new BigInteger(reversed);
    }

    private static byte[] ToBigEndianBytes(BigInteger value)
    {
        var bytes = value.ToByteArray();
        Array.Reverse(bytes);
        return bytes;
    }
}
