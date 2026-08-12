using System.Net;
using Ada.API.Interfaces.Networking.Client;
using Ada.Networking;
using Ada.Networking.Options;
using Moq;

namespace Ada.Tests.Networking;

[TestFixture]
public class NetworkListenerSlotTests
{
    private static NetworkListener Listener(int maxConnections, int maxPerAddress) =>
        new(
            Microsoft.Extensions.Options.Options.Create(new NetworkOptions
            {
                MaxConnections = maxConnections,
                MaxConnectionsPerAddress = maxPerAddress
            }),
            NullLogger<NetworkListener>.Instance,
            Mock.Of<INetworkClientFactory>(),
            Mock.Of<INetworkClientConnectionHandler>());

    [Test]
    public void Reserve_UpToThePerAddressCap_ThenRefuses()
    {
        var listener = Listener(maxConnections: 100, maxPerAddress: 3);
        var ip = IPAddress.Parse("10.0.0.1");

        Assert.Multiple(() =>
        {
            Assert.That(listener.TryReserveSlot(ip), Is.True);
            Assert.That(listener.TryReserveSlot(ip), Is.True);
            Assert.That(listener.TryReserveSlot(ip), Is.True);
            Assert.That(listener.TryReserveSlot(ip), Is.False, "the cap is three");
        });
    }

    [Test]
    public void Release_ReturnsTheSlotToTheAddress()
    {
        var listener = Listener(maxConnections: 100, maxPerAddress: 1);
        var ip = IPAddress.Parse("10.0.0.2");

        Assert.That(listener.TryReserveSlot(ip), Is.True);
        Assert.That(listener.TryReserveSlot(ip), Is.False);

        listener.ReleaseSlot(ip);

        Assert.That(listener.TryReserveSlot(ip), Is.True, "a released slot must be reusable");
    }

    [Test]
    public void ContendedReserveAndRelease_LeaksNoSlots()
    {
        const int perAddress = 8;
        const int workers = 16;
        const int iterations = 500;

        var listener = Listener(maxConnections: 0, maxPerAddress: perAddress);
        var ip = IPAddress.Parse("10.0.0.3");

        Parallel.For(0, workers, _ =>
        {
            for (var i = 0; i < iterations; i++)
            {
                if (listener.TryReserveSlot(ip))
                {
                    listener.ReleaseSlot(ip);
                }
            }
        });

        var granted = 0;

        for (var i = 0; i < perAddress; i++)
        {
            if (listener.TryReserveSlot(ip))
            {
                granted++;
            }
        }

        Assert.That(granted, Is.EqualTo(perAddress),
            "every slot must be free again once all reservations have been released");
    }

    [Test]
    public void ContendedReserve_NeverExceedsTheCap()
    {
        const int perAddress = 5;

        var listener = Listener(maxConnections: 0, maxPerAddress: perAddress);
        var ip = IPAddress.Parse("10.0.0.4");
        var granted = 0;

        Parallel.For(0, 200, _ =>
        {
            if (listener.TryReserveSlot(ip))
            {
                Interlocked.Increment(ref granted);
            }
        });

        Assert.That(granted, Is.EqualTo(perAddress));
    }

    [Test]
    public void ZeroPerAddressCap_DisablesTheLimit()
    {
        var listener = Listener(maxConnections: 0, maxPerAddress: 0);
        var ip = IPAddress.Parse("10.0.0.5");

        for (var i = 0; i < 50; i++)
        {
            Assert.That(listener.TryReserveSlot(ip), Is.True);
        }
    }
}
