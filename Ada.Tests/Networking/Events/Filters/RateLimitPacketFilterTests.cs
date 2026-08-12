using System.Net;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Networking.Events.Filters;
using Moq;

namespace Ada.Tests.Networking.Events.Filters;

[TestFixture]
public class RateLimitPacketFilterTests
{
    private static int _nextAddress;

    private static INetworkClient CreateClient()
    {
        var octet = Interlocked.Increment(ref _nextAddress) % 250 + 1;

        var client = new Mock<INetworkClient>();
        client.SetupGet(c => c.Guid).Returns(Guid.NewGuid());
        client.SetupGet(c => c.IpAddress).Returns(IPAddress.Parse($"10.0.0.{octet}"));

        return client.Object;
    }

    private static bool Allow(RateLimitPacketFilter filter, INetworkClient client)
        => filter.Allow(client, 1234, typeof(INetworkPacketEventHandler));

    [Test]
    public void Allow_BurstBeyondCapacity_EventuallyBlocks()
    {
        var filter = new RateLimitPacketFilter(NullLogger<RateLimitPacketFilter>.Instance);
        var client = CreateClient();

        var blocked = false;

        for (var i = 0; i < 1000; i++)
        {
            if (!Allow(filter, client))
            {
                blocked = true;
                break;
            }
        }

        Assert.That(blocked, Is.True);
    }

    [Test]
    public void Allow_DifferentAddresses_AreLimitedIndependently()
    {
        var filter = new RateLimitPacketFilter(NullLogger<RateLimitPacketFilter>.Instance);
        var first = CreateClient();
        var second = CreateClient();

        for (var i = 0; i < 1000; i++)
        {
            Allow(filter, first);
        }

        Assert.That(Allow(filter, second), Is.True);
    }
}
