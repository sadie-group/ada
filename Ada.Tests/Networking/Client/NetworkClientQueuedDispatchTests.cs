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
public class NetworkClientQueuedDispatchTests
{
    private sealed class ScriptedWebSocket : WebSocket
    {
        public WebSocketState CurrentState { get; set; } = WebSocketState.Open;

        public int Aborts { get; private set; }

        public override WebSocketState State => CurrentState;

        public override void Abort()
        {
            Aborts++;
            CurrentState = WebSocketState.Aborted;
        }

        public override ValueTask SendAsync(ReadOnlyMemory<byte> b, WebSocketMessageType m, bool e, CancellationToken t)
            => ValueTask.CompletedTask;

        public override Task SendAsync(ArraySegment<byte> b, WebSocketMessageType m, bool e, CancellationToken t)
            => Task.CompletedTask;

        public override Task CloseAsync(WebSocketCloseStatus s, string? d, CancellationToken t) => Task.CompletedTask;

        public override Task CloseOutputAsync(WebSocketCloseStatus s, string? d, CancellationToken t) => Task.CompletedTask;

        public override void Dispose() { }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> b, CancellationToken t)
            => throw new NotSupportedException();

        public override WebSocketCloseStatus? CloseStatus => null;

        public override string? CloseStatusDescription => null;

        public override string? SubProtocol => null;
    }

    private sealed class NeverThrottled : IPacketRateThrottle
    {
        public bool TryConsume(Guid clientGuid, IPAddress address) => true;

        public void Forget(Guid clientGuid) { }
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

    private static NetworkClientConnectionHandler Handler(
        IWebSocketMessageReader reader,
        INetworkPacketHandler packetHandler,
        IPacketRateThrottle? throttle = null)
        => new(
            NullLogger<NetworkClientConnectionHandler>.Instance,
            Mock.Of<INetworkClientRepository>(),
            reader,
            new PacketDispatcher(packetHandler, NullLogger<PacketDispatcher>.Instance),
            throttle ?? new NeverThrottled(),
            Mock.Of<IClientDisposalService>());

    private static (Mock<IPacketCodec> Codec, Func<short, INetworkPacket> Make) SequentialCodec()
    {
        var packets = new Dictionary<short, INetworkPacket>();

        INetworkPacket Make(short id)
        {
            if (packets.TryGetValue(id, out var existing))
            {
                return existing;
            }

            var packet = new Mock<INetworkPacket>();
            packet.SetupGet(p => p.PacketId).Returns(id);
            packets[id] = packet.Object;
            return packet.Object;
        }

        var decoder = new Mock<INetworkPacketDecoder>();
        decoder.Setup(d => d.Decode(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns((Guid _, byte[] buffer, int _) => Make(buffer[0]));

        var codec = new Mock<IPacketCodec>();
        codec.SetupGet(c => c.Decoder).Returns(decoder.Object);

        return (codec, Make);
    }

    [Test]
    public async Task QueuedPackets_AreHandledInArrivalOrder()
    {
        var socket = new ScriptedWebSocket();
        var (codec, _) = SequentialCodec();
        var client = MakeClient(Guid.NewGuid(), socket, codec.Object);

        const int total = 64;
        var next = 0;

        var reader = new Mock<IWebSocketMessageReader>();
        reader.Setup(r => r.ReadMessageAsync(socket, It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                if (next == total)
                {
                    socket.CurrentState = WebSocketState.Closed;
                    return new ValueTask<(byte[], int)>((Array.Empty<byte>(), 0));
                }

                return new ValueTask<(byte[], int)>((new[] { (byte) next++ }, 1));
            });

        var observed = new List<short>();
        var packetHandler = new Mock<INetworkPacketHandler>();
        packetHandler
            .Setup(h => h.HandleAsync(It.IsAny<INetworkClient>(), It.IsAny<INetworkPacket>()))
            .Returns(async (INetworkClient _, INetworkPacket packet) =>
            {
                await Task.Yield();
                observed.Add(packet.PacketId);
            });

        await Handler(reader.Object, packetHandler.Object).HandleClientAsync(client.Object, CancellationToken.None);

        Assert.That(observed, Is.EqualTo(Enumerable.Range(0, total).Select(x => (short) x).ToList()),
            "a single connection's packets must stay in the order they arrived");
    }

    [Test]
    public async Task SlowHandler_DoesNotStopTheReadLoop()
    {
        var socket = new ScriptedWebSocket();
        var (codec, _) = SequentialCodec();
        var client = MakeClient(Guid.NewGuid(), socket, codec.Object);

        const int total = 16;
        var next = 0;
        var release = new TaskCompletionSource();
        var firstHandlerEntered = new TaskCompletionSource();
        var allFramesRead = new TaskCompletionSource();

        var reader = new Mock<IWebSocketMessageReader>();
        reader.Setup(r => r.ReadMessageAsync(socket, It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                if (next == total)
                {
                    allFramesRead.TrySetResult();
                    socket.CurrentState = WebSocketState.Closed;
                    return new ValueTask<(byte[], int)>((Array.Empty<byte>(), 0));
                }

                return new ValueTask<(byte[], int)>((new[] { (byte) next++ }, 1));
            });

        var packetHandler = new Mock<INetworkPacketHandler>();
        packetHandler
            .Setup(h => h.HandleAsync(It.IsAny<INetworkClient>(), It.IsAny<INetworkPacket>()))
            .Returns(async (INetworkClient _, INetworkPacket packet) =>
            {
                if (packet.PacketId != 0)
                {
                    return;
                }

                firstHandlerEntered.TrySetResult();
                await release.Task;
            });

        var run = Handler(reader.Object, packetHandler.Object).HandleClientAsync(client.Object, CancellationToken.None);

        await firstHandlerEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await allFramesRead.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(next, Is.EqualTo(total),
            "every frame must be read off the socket while the first handler is still running");

        release.SetResult();

        await run.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task QueueOverflow_AbortsTheConnectionInsteadOfGrowing()
    {
        var socket = new ScriptedWebSocket();
        var (codec, _) = SequentialCodec();
        var client = MakeClient(Guid.NewGuid(), socket, codec.Object);

        var release = new TaskCompletionSource();
        var frames = 0;

        var reader = new Mock<IWebSocketMessageReader>();
        reader.Setup(r => r.ReadMessageAsync(socket, It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                frames++;
                return new ValueTask<(byte[], int)>((new byte[] { 1 }, 1));
            });

        var handled = 0;
        var packetHandler = new Mock<INetworkPacketHandler>();
        packetHandler
            .Setup(h => h.HandleAsync(It.IsAny<INetworkClient>(), It.IsAny<INetworkPacket>()))
            .Returns(async (INetworkClient _, INetworkPacket _) =>
            {
                handled++;
                await release.Task;
            });

        var run = Handler(reader.Object, packetHandler.Object).HandleClientAsync(client.Object, CancellationToken.None);

        await TestContext.Out.WriteLineAsync("waiting for the queue to fill");

        var waited = 0;

        while (socket.Aborts == 0 && waited < 5000)
        {
            await Task.Delay(10);
            waited += 10;
        }

        Assert.Multiple(() =>
        {
            Assert.That(socket.Aborts, Is.EqualTo(1), "a client that outruns the queue is disconnected");
            Assert.That(handled, Is.EqualTo(1), "the consumer is still stuck on the first packet");
            Assert.That(frames, Is.LessThan(1_000), "the read loop stops instead of buffering without bound");
        });

        release.SetResult();

        await run.WaitAsync(TimeSpan.FromSeconds(5));
    }
}
