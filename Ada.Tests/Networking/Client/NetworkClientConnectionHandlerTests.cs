using System.Net;
using System.Net.WebSockets;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Packets;
using Ada.Networking.Client;
using Ada.Networking.Packets;
using Moq;

namespace Ada.Tests.Networking.Client;

[TestFixture]
public class NetworkClientConnectionHandlerTests
{
    private sealed class StatefulWebSocket : WebSocket
    {
        public WebSocketState CurrentState { get; set; } = WebSocketState.Open;

        public override WebSocketState State => CurrentState;

        public override ValueTask SendAsync(ReadOnlyMemory<byte> b, WebSocketMessageType m, bool e, CancellationToken t)
            => ValueTask.CompletedTask;

        public override Task SendAsync(ArraySegment<byte> b, WebSocketMessageType m, bool e, CancellationToken t)
            => Task.CompletedTask;

        public override void Abort() { }

        public override Task CloseAsync(WebSocketCloseStatus s, string? d, CancellationToken t) => Task.CompletedTask;

        public override Task CloseOutputAsync(WebSocketCloseStatus s, string? d, CancellationToken t) => Task.CompletedTask;

        public override void Dispose() { }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> b, CancellationToken t)
            => throw new NotSupportedException();

        public override WebSocketCloseStatus? CloseStatus => null;

        public override string? CloseStatusDescription => null;

        public override string? SubProtocol => null;
    }

    private static Mock<INetworkClient> MakeClient(Guid guid, WebSocket socket, IPacketCodec codec)
    {
        var client = new Mock<INetworkClient>();
        client.SetupGet(c => c.Guid).Returns(guid);
        client.SetupGet(c => c.WebSocket).Returns(socket);
        client.SetupGet(c => c.Codec).Returns(codec);
        client.SetupGet(c => c.IpAddress).Returns(IPAddress.Loopback);
        return client;
    }

    [Test]
    public async Task HandleClientAsync_ProcessesMessagesUntilSocketCloses()
    {
        var socket = new StatefulWebSocket();
        var guid = Guid.NewGuid();
        var payload = new byte[] { 1, 2, 3 };
        var packet = Mock.Of<INetworkPacket>();

        var decoder = new Mock<INetworkPacketDecoder>();
        decoder.Setup(d => d.Decode(guid, payload, 3)).Returns(packet);
        var codec = new Mock<IPacketCodec>();
        codec.SetupGet(c => c.Decoder).Returns(decoder.Object);

        var client = MakeClient(guid, socket, codec.Object);

        var calls = 0;
        var reader = new Mock<IWebSocketMessageReader>();
        reader.Setup(r => r.ReadMessageAsync(socket, It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                calls++;

                if (calls == 1)
                {
                    return new ValueTask<(byte[] buffer, int length)>((Array.Empty<byte>(), 0));
                }

                socket.CurrentState = WebSocketState.Closed;
                return new ValueTask<(byte[] buffer, int length)>((payload, 3));
            });

        var packetHandler = new Mock<INetworkPacketHandler>();
        var repository = new Mock<INetworkClientRepository>();
        var disposal = new Mock<IClientDisposalService>();

        var handler = new NetworkClientConnectionHandler(
            NullLogger<NetworkClientConnectionHandler>.Instance,
            repository.Object,
            reader.Object,
            new PacketDispatcher(packetHandler.Object, NullLogger<PacketDispatcher>.Instance),
            new PacketRateThrottle(),
            disposal.Object);

        await handler.HandleClientAsync(client.Object, CancellationToken.None);

        Assert.That(calls, Is.EqualTo(2));
        repository.Verify(r => r.AddClient(guid, client.Object), Times.Once);
        packetHandler.Verify(h => h.HandleAsync(client.Object, packet), Times.Once);
        disposal.Verify(d => d.HandleDisconnectAsync(client.Object), Times.Once);
    }

    [Test]
    public async Task HandleClientAsync_CancelledToken_SkipsLoopAndDisposes()
    {
        var socket = new StatefulWebSocket();
        var guid = Guid.NewGuid();
        var client = MakeClient(guid, socket, Mock.Of<IPacketCodec>());

        var reader = new Mock<IWebSocketMessageReader>();
        var repository = new Mock<INetworkClientRepository>();
        var disposal = new Mock<IClientDisposalService>();

        var handler = new NetworkClientConnectionHandler(
            NullLogger<NetworkClientConnectionHandler>.Instance,
            repository.Object,
            reader.Object,
            new PacketDispatcher(Mock.Of<INetworkPacketHandler>(), NullLogger<PacketDispatcher>.Instance),
            new PacketRateThrottle(),
            disposal.Object);

        await handler.HandleClientAsync(client.Object, new CancellationToken(true));

        reader.Verify(r => r.ReadMessageAsync(It.IsAny<WebSocket>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(r => r.AddClient(guid, client.Object), Times.Once);
        disposal.Verify(d => d.HandleDisconnectAsync(client.Object), Times.Once);
    }
}
