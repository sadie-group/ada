using Ada.API.Interfaces.Networking.Client;
using Ada.Networking;
using Ada.Networking.Options;
using Moq;

namespace Ada.Tests.Networking;

[TestFixture]
public class NetworkListenerOriginTests
{
    private static NetworkListener Listener(string? allowedOrigins) =>
        new(
            Microsoft.Extensions.Options.Options.Create(new NetworkOptions
            {
                AllowedOrigins = allowedOrigins
            }),
            NullLogger<NetworkListener>.Instance,
            Mock.Of<INetworkClientFactory>(),
            Mock.Of<INetworkClientConnectionHandler>());

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void EmptyAllowlist_RefusesEveryOrigin(string? allowedOrigins)
    {
        var listener = Listener(allowedOrigins);

        Assert.Multiple(() =>
        {
            Assert.That(listener.IsAllowedOrigin("https://hotel.example"), Is.False);
            Assert.That(listener.IsAllowedOrigin(null), Is.False);
            Assert.That(listener.IsAllowedOrigin(""), Is.False);
        });
    }

    [Test]
    public void Wildcard_AcceptsAnyOriginIncludingAbsentOnes()
    {
        var listener = Listener("*");

        Assert.Multiple(() =>
        {
            Assert.That(listener.IsAllowedOrigin("https://anything.example"), Is.True);
            Assert.That(listener.IsAllowedOrigin(null), Is.True, "a non-browser client sends no Origin");
        });
    }

    [Test]
    public void NamedOrigins_MatchCaseInsensitivelyAndIgnoreSurroundingSpace()
    {
        var listener = Listener("https://hotel.example , https://www.hotel.example");

        Assert.Multiple(() =>
        {
            Assert.That(listener.IsAllowedOrigin("https://hotel.example"), Is.True);
            Assert.That(listener.IsAllowedOrigin("HTTPS://WWW.HOTEL.EXAMPLE"), Is.True);
            Assert.That(listener.IsAllowedOrigin("https://evil.example"), Is.False);
            Assert.That(listener.IsAllowedOrigin(null), Is.False,
                "an allowlist that names origins must not fall back to accepting a missing header");
        });
    }

    [Test]
    public void NamedOrigins_DoNotMatchOnSubstring()
    {
        var listener = Listener("https://hotel.example");

        Assert.Multiple(() =>
        {
            Assert.That(listener.IsAllowedOrigin("https://hotel.example.evil.test"), Is.False);
            Assert.That(listener.IsAllowedOrigin("https://not-hotel.example"), Is.False);
        });
    }
}
