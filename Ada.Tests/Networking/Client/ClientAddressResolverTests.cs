using System.Net;
using Ada.Networking.Client;

namespace Ada.Tests.Networking.Client;

[TestFixture]
public class ClientAddressResolverTests
{
    private static IPAddress Ip(string value) => IPAddress.Parse(value);

    [Test]
    public void Resolve_NoTrustedProxies_IgnoresForwardedHeader()
    {
        var resolver = new ClientAddressResolver(null);

        var resolved = resolver.Resolve(Ip("203.0.113.9"), "1.2.3.4");

        Assert.Multiple(() =>
        {
            Assert.That(resolver.HasTrustedProxies, Is.False);
            Assert.That(resolved, Is.EqualTo(Ip("203.0.113.9")),
                "an unconfigured deployment must never believe a header the client can set");
        });
    }

    [Test]
    public void Resolve_UntrustedPeerSendingHeader_UsesPeerAddress()
    {
        var resolver = new ClientAddressResolver("10.0.0.1");
        var resolved = resolver.Resolve(Ip("203.0.113.9"), "198.51.100.7");

        Assert.That(resolved, Is.EqualTo(Ip("203.0.113.9")));
    }

    [Test]
    public void Resolve_TrustedProxy_UsesForwardedClientAddress()
    {
        var resolver = new ClientAddressResolver("10.0.0.1");

        var resolved = resolver.Resolve(Ip("10.0.0.1"), "198.51.100.7");

        Assert.That(resolved, Is.EqualTo(Ip("198.51.100.7")));
    }

    [Test]
    public void Resolve_TrustedProxy_TakesRightmostUntrustedHop()
    {
        var resolver = new ClientAddressResolver("10.0.0.0/8");
        var resolved = resolver.Resolve(Ip("10.0.0.1"), "1.1.1.1, 2.2.2.2, 198.51.100.7, 10.0.0.5");

        Assert.That(resolved, Is.EqualTo(Ip("198.51.100.7")));
    }

    [Test]
    public void Resolve_CidrRange_MatchesProxiesInsideRange()
    {
        var resolver = new ClientAddressResolver("172.16.0.0/12");

        Assert.Multiple(() =>
        {
            Assert.That(resolver.IsTrustedProxy(Ip("172.16.4.9")), Is.True);
            Assert.That(resolver.IsTrustedProxy(Ip("172.32.0.1")), Is.False);
            Assert.That(resolver.Resolve(Ip("172.16.4.9"), "198.51.100.7"), Is.EqualTo(Ip("198.51.100.7")));
        });
    }

    [Test]
    public void Resolve_AllHopsTrusted_FallsBackToPeer()
    {
        var resolver = new ClientAddressResolver("10.0.0.0/8");

        var resolved = resolver.Resolve(Ip("10.0.0.1"), "10.0.0.4, 10.0.0.5");

        Assert.That(resolved, Is.EqualTo(Ip("10.0.0.1")));
    }

    [Test]
    public void Resolve_MalformedHeader_FallsBackToPeer()
    {
        var resolver = new ClientAddressResolver("10.0.0.1");

        Assert.Multiple(() =>
        {
            Assert.That(resolver.Resolve(Ip("10.0.0.1"), "not-an-address"), Is.EqualTo(Ip("10.0.0.1")));
            Assert.That(resolver.Resolve(Ip("10.0.0.1"), ""), Is.EqualTo(Ip("10.0.0.1")));
        });
    }

    [Test]
    public void Resolve_ForwardedEntryWithPort_StripsThePort()
    {
        var resolver = new ClientAddressResolver("10.0.0.1");

        Assert.That(resolver.Resolve(Ip("10.0.0.1"), "198.51.100.7:52344"), Is.EqualTo(Ip("198.51.100.7")));
    }

    [Test]
    public void Resolve_IpV6MappedPeer_IsNormalisedBeforeMatching()
    {
        var resolver = new ClientAddressResolver("10.0.0.1");

        var resolved = resolver.Resolve(Ip("10.0.0.1").MapToIPv6(), "198.51.100.7");

        Assert.That(resolved, Is.EqualTo(Ip("198.51.100.7")));
    }

    [Test]
    public void Resolve_NullPeer_DoesNotThrow()
    {
        var resolver = new ClientAddressResolver("10.0.0.1");

        Assert.That(resolver.Resolve(null, null), Is.EqualTo(IPAddress.None));
    }
}
