using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Packets;
using Ada.Networking.Packets;
using Moq;

namespace Ada.Tests.Networking.Packets;

[TestFixture]
public class PacketDispatcherTests
{
    [Test]
    public async Task Enqueue_DispatchesPacketToHandler()
    {
        var handled = new TaskCompletionSource();
        var client = Mock.Of<INetworkClient>();
        var packet = Mock.Of<INetworkPacket>();

        var handler = new Mock<INetworkPacketHandler>();
        handler
            .Setup(h => h.HandleAsync(client, packet))
            .Returns(Task.CompletedTask)
            .Callback(() => handled.TrySetResult());

        new PacketDispatcher(handler.Object).Enqueue(client, packet);

        await handled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        handler.Verify(h => h.HandleAsync(client, packet), Times.Once);
    }

    [Test]
    public async Task Enqueue_HandlerThrows_DispatcherKeepsProcessing()
    {
        var second = new TaskCompletionSource();
        var client = Mock.Of<INetworkClient>();
        var first = Mock.Of<INetworkPacket>();
        var next = Mock.Of<INetworkPacket>();

        var handler = new Mock<INetworkPacketHandler>();
        handler.Setup(h => h.HandleAsync(client, first)).ThrowsAsync(new InvalidOperationException());
        handler.Setup(h => h.HandleAsync(client, next))
            .Returns(Task.CompletedTask)
            .Callback(() => second.TrySetResult());

        var dispatcher = new PacketDispatcher(handler.Object, workers: 1);
        dispatcher.Enqueue(client, first);
        dispatcher.Enqueue(client, next);

        await second.Task.WaitAsync(TimeSpan.FromSeconds(5));
        handler.Verify(h => h.HandleAsync(client, next), Times.Once);
    }
}
