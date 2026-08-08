using System.Numerics;
using Ada.Networking.Encryption.Extensions;

namespace Ada.Networking.Encryption;

public class DiffieHellman
{
    private const int _dhPrimesBitSize = 256;
    private const int _dhKeyBitSize = 256;

    public BigInteger Prime { get; private set; }
    public BigInteger Generator { get; private set; }

    private BigInteger _privateKey;
    public BigInteger PublicKey { get; private set; }

    public DiffieHellman()
    {
        GeneratePrimes();
        GenerateKeys();
    }

    public DiffieHellman(BigInteger prime, BigInteger generator)
    {
        Prime = prime;
        Generator = generator;
        GenerateKeys();
    }

    private void GeneratePrimes()
    {
        Prime = BigIntegerExtensions.GeneratePseudoPrime(_dhPrimesBitSize, 10);
        Generator = BigIntegerExtensions.GeneratePseudoPrime(_dhPrimesBitSize, 10);

        if (Generator > Prime)
        {
            (Generator, Prime) = (Prime, Generator);
        }
    }

    private void GenerateKeys()
    {
        _privateKey = BigIntegerExtensions.GeneratePseudoPrime(_dhKeyBitSize, 10);
        PublicKey = BigInteger.ModPow(Generator, _privateKey, Prime);
    }

    public bool TryCalculateSharedKey(BigInteger publicKey, out BigInteger sharedKey)
    {
        sharedKey = BigInteger.Zero;

        if (publicKey <= BigInteger.One || publicKey >= Prime - BigInteger.One)
        {
            return false;
        }

        var candidate = BigInteger.ModPow(publicKey, _privateKey, Prime);

        if (candidate <= BigInteger.One || candidate == Prime - BigInteger.One)
        {
            return false;
        }

        sharedKey = candidate;
        return true;
    }
}
