using System.Net;
using System.Net.WebSockets;
using Ada.API;
using Ada.API.Interfaces.Networking.Packets;
using Ada.Networking.Client;
using Ada.Networking.Packets;
using Moq;

namespace Ada.Tests.Networking.Client;

[TestFixture]
public class NetworkClientTests
{
    private sealed class GatedWebSocket : WebSocket
    {
        private readonly object _lock = new();
        private TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<byte[]> Frames { get; } = [];

        public bool Gated { get; set; }

        public void Release()
        {
            lock (_lock)
            {
                _gate.TrySetResult();
                _gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }

        public override async ValueTask SendAsync(
            ReadOnlyMemory<byte> buffer,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken)
        {
            Task gate;

            lock (_lock)
            {
                gate = Gated ? _gate.Task : Task.CompletedTask;
            }

            await gate;

            lock (_lock)
            {
                Frames.Add(buffer.ToArray());
            }
        }

        public override Task SendAsync(
            ArraySegment<byte> buffer,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken)
            => SendAsync((ReadOnlyMemory<byte>) buffer.AsMemory(), messageType, endOfMessage, cancellationToken).AsTask();

        public override WebSocketState State { get; } = WebSocketState.Open;

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

    private static NetworkClient CreateClient(WebSocket socket)
    {
        var registry = new Mock<IPacketCodecRegistry>();
        registry.SetupGet(x => x.Default).Returns(TestPacketCodec.Instance);

        return new NetworkClient(
            NullLogger<NetworkClient>.Instance,
            registry.Object,
            Microsoft.Extensions.Options.Options.Create(new Ada.Networking.Options.NetworkOptions()),
            IPAddress.Loopback,
            Guid.NewGuid(),
            socket);
    }

    private static INetworkPacketWriter PacketOf(byte marker)
    {
        var writer = new NetworkPacketWriter();
        writer.WriteByte(marker);
        return writer;
    }

    [Test]
    public async Task WriteToStreamAsync_PacketsQueuedDuringSend_CoalesceIntoOneFrame()
    {
        var socket = new GatedWebSocket { Gated = true };
        var client = CreateClient(socket);
        
        await client.WriteToStreamAsync(PacketOf(1));
        await client.WriteToStreamAsync(PacketOf(2));
        await client.WriteToStreamAsync(PacketOf(3));
        await client.WriteToStreamAsync(PacketOf(4));

        socket.Gated = false;
        socket.Release();

        await WaitForFramesAsync(socket, 2);

        Assert.That(socket.Frames, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(socket.Frames[0], Has.Length.EqualTo(5), "first packet goes out on its own");
            Assert.That(socket.Frames[1], Has.Length.EqualTo(15), "the other three coalesce");
        });
    }

    [Test]
    public async Task WriteToStreamAsync_PreservesOrderAcrossCoalescing()
    {
        var socket = new GatedWebSocket { Gated = true };
        var client = CreateClient(socket);

        for (byte i = 1; i <= 6; i++)
        {
            await client.WriteToStreamAsync(PacketOf(i));
        }

        socket.Gated = false;
        socket.Release();

        await WaitForBytesAsync(socket, 30);

        var payload = socket.Frames.SelectMany(x => x).ToArray();
        var markers = new List<byte>();

        for (var offset = 0; offset < payload.Length; offset += 5)
        {
            markers.Add(payload[offset + 4]);
        }

        Assert.That(markers, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public async Task QueueOutbound_DoesNotSendUntilFlushed()
    {
        var socket = new GatedWebSocket();
        var client = CreateClient(socket);

        client.QueueOutbound(PacketOf(1));
        client.QueueOutbound(PacketOf(2));

        Assert.That(socket.Frames, Is.Empty);

        await client.FlushAsync();

        Assert.That(socket.Frames, Has.Count.EqualTo(1));
        Assert.That(socket.Frames[0], Has.Length.EqualTo(10));
    }

    [Test]
    public async Task DisposeAsync_DrainsQueuedPacketsBeforeClosing()
    {
        var socket = new GatedWebSocket();
        var client = CreateClient(socket);

        client.QueueOutbound(PacketOf(1));

        await client.DisposeAsync();

        Assert.That(socket.Frames, Has.Count.EqualTo(1));
    }

    private static async Task WaitForFramesAsync(GatedWebSocket socket, int count)
    {
        for (var i = 0; i < 200 && socket.Frames.Count < count; i++)
        {
            await Task.Delay(10);
        }
    }

    private static async Task WaitForBytesAsync(GatedWebSocket socket, int bytes)
    {
        for (var i = 0; i < 200 && socket.Frames.Sum(x => x.Length) < bytes; i++)
        {
            await Task.Delay(10);
        }
    }
}
