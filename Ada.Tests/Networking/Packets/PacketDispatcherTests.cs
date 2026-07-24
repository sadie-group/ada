using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Packets;
using Ada.Networking.Packets;
using Moq;

namespace Ada.Tests.Networking.Packets;

[TestFixture]
public class PacketDispatcherTests
{
    [Test]
    public async Task ProcessAsync_DispatchesPacketToHandler()
    {
        var client = Mock.Of<INetworkClient>();
        var packet = Mock.Of<INetworkPacket>();

        var handler = new Mock<INetworkPacketHandler>();
        handler.Setup(h => h.HandleAsync(client, packet)).Returns(Task.CompletedTask);

        await new PacketDispatcher(handler.Object).ProcessAsync(client, packet);

        handler.Verify(h => h.HandleAsync(client, packet), Times.Once);
    }

    [Test]
    public async Task ProcessAsync_HandlerThrows_DoesNotPropagate()
    {
        var client = Mock.Of<INetworkClient>();
        var packet = Mock.Of<INetworkPacket>();

        var handler = new Mock<INetworkPacketHandler>();
        handler.Setup(h => h.HandleAsync(client, packet)).ThrowsAsync(new InvalidOperationException());

        var dispatcher = new PacketDispatcher(handler.Object);

        Assert.DoesNotThrowAsync(() => dispatcher.ProcessAsync(client, packet));
        await Task.CompletedTask;
    }
}
