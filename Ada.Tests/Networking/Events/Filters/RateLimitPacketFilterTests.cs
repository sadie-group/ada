using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Networking.Events.Filters;
using Moq;

namespace Ada.Tests.Networking.Events.Filters;

[TestFixture]
public class RateLimitPacketFilterTests
{
    private static INetworkClient CreateClient()
    {
        var client = new Mock<INetworkClient>();
        client.SetupGet(c => c.Guid).Returns(Guid.NewGuid());
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

        for (var i = 0; i < 500; i++)
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
    public void Allow_DifferentClients_AreLimitedIndependently()
    {
        var filter = new RateLimitPacketFilter(NullLogger<RateLimitPacketFilter>.Instance);
        var first = CreateClient();
        var second = CreateClient();

        for (var i = 0; i < 500; i++)
        {
            Allow(filter, first);
        }

        Assert.That(Allow(filter, second), Is.True);
    }
}
