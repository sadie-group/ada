using System.Net.WebSockets;
using Ada.Networking;
using Moq;

namespace Ada.Tests.Networking;

[TestFixture]
public class WebSocketMessageReaderTests
{
    private static Mock<WebSocket> SocketReturning(params (byte[] data, bool endOfMessage, WebSocketMessageType type)[] frames)
    {
        var socket = new Mock<WebSocket>();
        var call = 0;

        socket
            .Setup(s => s.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
            .Returns((ArraySegment<byte> buffer, CancellationToken _) =>
            {
                var (data, end, type) = frames[call++];
                data.CopyTo(buffer.Array!, buffer.Offset);
                return Task.FromResult(new WebSocketReceiveResult(data.Length, type, end));
            });

        return socket;
    }

    [Test]
    public async Task ReadMessageAsync_SingleFrame_ReturnsFrameBytes()
    {
        var socket = SocketReturning(([1, 2, 3], true, WebSocketMessageType.Binary));

        var (buffer, length) = await new WebSocketMessageReader().ReadMessageAsync(socket.Object, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(length, Is.EqualTo(3));
            Assert.That(buffer.Take(3), Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public async Task ReadMessageAsync_MultipleFrames_ConcatenatesUntilEndOfMessage()
    {
        var socket = SocketReturning(
            ([1, 2], false, WebSocketMessageType.Binary),
            ([3, 4], true, WebSocketMessageType.Binary));

        var (buffer, length) = await new WebSocketMessageReader().ReadMessageAsync(socket.Object, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(length, Is.EqualTo(4));
            Assert.That(buffer.Take(4), Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        });
    }

    [Test]
    public async Task ReadMessageAsync_MessageLargerThanInitialBuffer_GrowsBuffer()
    {
        var chunk = Enumerable.Range(0, 3000).Select(i => (byte)i).ToArray();
        var socket = SocketReturning(
            (chunk, false, WebSocketMessageType.Binary),
            (chunk, false, WebSocketMessageType.Binary),
            (chunk, true, WebSocketMessageType.Binary));

        var (buffer, length) = await new WebSocketMessageReader().ReadMessageAsync(socket.Object, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(length, Is.EqualTo(9000));
            Assert.That(buffer.Take(3000), Is.EqualTo(chunk));
            Assert.That(buffer.Skip(6000).Take(3000), Is.EqualTo(chunk));
        });
    }

    [Test]
    public async Task ReadMessageAsync_CloseMessage_ReturnsEmpty()
    {
        var socket = SocketReturning(([], true, WebSocketMessageType.Close));

        var (buffer, length) = await new WebSocketMessageReader().ReadMessageAsync(socket.Object, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(length, Is.Zero);
            Assert.That(buffer, Is.Empty);
        });
    }

    [Test]
    public void ReadMessageAsync_SocketThrows_PropagatesException()
    {
        var socket = new Mock<WebSocket>();
        socket
            .Setup(s => s.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new WebSocketException("boom"));

        Assert.ThrowsAsync<WebSocketException>(async () =>
            await new WebSocketMessageReader().ReadMessageAsync(socket.Object, CancellationToken.None));
    }
}
