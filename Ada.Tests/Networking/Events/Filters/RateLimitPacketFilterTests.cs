using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Networking.Events.Filters;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Ada.Tests.Networking.Events.Filters;

[TestFixture]
public class RateLimitPacketFilterTests
{
    private static (INetworkClient client, INetworkPacketEventHandler handler) CreatePair()
    {
        var client = new Mock<INetworkClient>();
        client.SetupGet(c => c.Guid).Returns(Guid.NewGuid());
        return (client.Object, Mock.Of<INetworkPacketEventHandler>());
    }

    [Test]
    public async Task AllowAsync_BurstBeyondCapacity_EventuallyBlocks()
    {
        var filter = new RateLimitPacketFilter(NullLogger<RateLimitPacketFilter>.Instance);
        var (client, handler) = CreatePair();

        var blocked = false;

        for (var i = 0; i < 500; i++)
        {
            if (!await filter.AllowAsync(client, handler))
            {
                blocked = true;
                break;
            }
        }

        Assert.That(blocked, Is.True);
    }

    [Test]
    public async Task AllowAsync_DifferentClients_AreLimitedIndependently()
    {
        var filter = new RateLimitPacketFilter(NullLogger<RateLimitPacketFilter>.Instance);
        var (first, handler) = CreatePair();
        var (second, _) = CreatePair();

        for (var i = 0; i < 500; i++)
        {
            await filter.AllowAsync(first, handler);
        }

        Assert.That(await filter.AllowAsync(second, handler), Is.True);
    }
}
