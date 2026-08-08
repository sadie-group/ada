using System.Numerics;
using Ada.Networking.Encryption;

namespace Ada.Tests.Networking.Encryption;

[TestFixture]
public class DiffieHellmanTests
{
    [Test]
    public void CalculateSharedKey_TwoPartiesWithSharedParameters_Agree()
    {
        var prime = BigInteger.Parse("170141183460469231731687303715884105727");
        var generator = new BigInteger(5);

        var alice = new DiffieHellman(prime, generator);
        var bob = new DiffieHellman(prime, generator);

        Assert.Multiple(() =>
        {
            Assert.That(alice.TryCalculateSharedKey(bob.PublicKey, out var aliceShared), Is.True);
            Assert.That(bob.TryCalculateSharedKey(alice.PublicKey, out var bobShared), Is.True);
            Assert.That(aliceShared, Is.EqualTo(bobShared));
        });
    }

    [TestCase("0")]
    [TestCase("1")]
    public void TryCalculateSharedKey_DegeneratePeerKey_IsRejected(string peerKey)
    {
        var prime = BigInteger.Parse("170141183460469231731687303715884105727");
        var dh = new DiffieHellman(prime, new BigInteger(5));

        Assert.That(dh.TryCalculateSharedKey(BigInteger.Parse(peerKey), out _), Is.False);
    }

    [Test]
    public void TryCalculateSharedKey_PeerKeyAtModulusBoundary_IsRejected()
    {
        var prime = BigInteger.Parse("170141183460469231731687303715884105727");
        var dh = new DiffieHellman(prime, new BigInteger(5));

        Assert.That(dh.TryCalculateSharedKey(prime - BigInteger.One, out _), Is.False);
    }

    [Test]
    public void Constructor_WithParameters_ExposesThem()
    {
        var prime = new BigInteger(23);
        var generator = new BigInteger(5);

        var dh = new DiffieHellman(prime, generator);

        Assert.Multiple(() =>
        {
            Assert.That(dh.Prime, Is.EqualTo(prime));
            Assert.That(dh.Generator, Is.EqualTo(generator));
        });
    }

    [Test]
    public void PublicKey_IsWithinModulus()
    {
        var prime = BigInteger.Parse("170141183460469231731687303715884105727");
        var dh = new DiffieHellman(prime, new BigInteger(5));

        Assert.Multiple(() =>
        {
            Assert.That(dh.PublicKey, Is.GreaterThanOrEqualTo(BigInteger.Zero));
            Assert.That(dh.PublicKey, Is.LessThan(prime));
        });
    }

    [Test]
    public void Constructor_Default_GeneratorNotGreaterThanPrime()
    {
        var dh = new DiffieHellman();
        Assert.That(dh.Generator, Is.LessThanOrEqualTo(dh.Prime));
    }
}
