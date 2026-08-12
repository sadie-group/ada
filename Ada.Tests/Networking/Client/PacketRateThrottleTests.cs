using System.Net;
using Ada.Networking.Client;

namespace Ada.Tests.Networking.Client;

[TestFixture]
public class PacketRateThrottleTests
{
    [Test]
    public void TryConsume_WithinConnectionBurst_Allows()
    {
        var throttle = new PacketRateThrottle();
        var guid = Guid.NewGuid();

        for (var i = 0; i < 120; i++)
        {
            Assert.That(throttle.TryConsume(guid, IPAddress.Loopback), Is.True, $"packet {i} should be allowed");
        }
    }

    [Test]
    public void TryConsume_BeyondConnectionBurst_Rejects()
    {
        var throttle = new PacketRateThrottle();
        var guid = Guid.NewGuid();

        for (var i = 0; i < 120; i++)
        {
            throttle.TryConsume(guid, IPAddress.Loopback);
        }

        Assert.That(throttle.TryConsume(guid, IPAddress.Loopback), Is.False);
    }

    [Test]
    public void TryConsume_ManyConnectionsFromOneAddress_IsBoundedByAddressBudget()
    {
        var throttle = new PacketRateThrottle();
        var address = IPAddress.Parse("203.0.113.7");

        var allowed = 0;

        for (var connection = 0; connection < 20; connection++)
        {
            var guid = Guid.NewGuid();

            for (var i = 0; i < 120; i++)
            {
                if (throttle.TryConsume(guid, address))
                {
                    allowed++;
                }
            }
        }

        Assert.That(allowed, Is.LessThan(600),
            "twenty fresh connections should not each get a full per-connection budget from one address");
    }

    [Test]
    public void TryConsume_SeparateAddresses_HaveSeparateBudgets()
    {
        var throttle = new PacketRateThrottle();
        var first = IPAddress.Parse("203.0.113.7");
        var second = IPAddress.Parse("203.0.113.8");

        for (var connection = 0; connection < 20; connection++)
        {
            var guid = Guid.NewGuid();

            for (var i = 0; i < 120; i++)
            {
                throttle.TryConsume(guid, first);
            }
        }

        Assert.That(throttle.TryConsume(Guid.NewGuid(), second), Is.True);
    }

    [Test]
    public void Forget_ResetsConnectionBudget()
    {
        var throttle = new PacketRateThrottle();
        var guid = Guid.NewGuid();

        for (var i = 0; i < 120; i++)
        {
            throttle.TryConsume(guid, IPAddress.Loopback);
        }

        Assert.That(throttle.TryConsume(guid, IPAddress.Loopback), Is.False);

        throttle.Forget(guid);

        Assert.That(throttle.TryConsume(guid, IPAddress.Loopback), Is.True);
    }
}
