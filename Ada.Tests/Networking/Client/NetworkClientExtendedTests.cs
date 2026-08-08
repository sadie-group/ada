using System.Net;
using System.Net.WebSockets;
using Ada.API;
using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Packets;
using Ada.API.Interfaces.Plugins;
using Ada.Db;
using Ada.Networking.Client;
using Ada.Networking.Packets;
using AutoMapper;
using Moq;

namespace Ada.Tests.Networking.Client;

[TestFixture]
public class NetworkClientExtendedTests
{
    private sealed class FakeWebSocket : WebSocket
    {
        public List<byte[]> Frames { get; } = [];
        public int AbortCount { get; private set; }
        public int CloseCalls { get; private set; }
        public bool ThrowOnSend { get; set; }
        public bool ThrowOnClose { get; set; }
        public WebSocketState CurrentState { get; set; } = WebSocketState.Open;

        public override WebSocketState State => CurrentState;

        public override ValueTask SendAsync(
            ReadOnlyMemory<byte> buffer,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken)
        {
            if (ThrowOnSend)
            {
                throw new WebSocketException(WebSocketError.Faulted);
            }

            lock (Frames)
            {
                Frames.Add(buffer.ToArray());
            }

            return ValueTask.CompletedTask;
        }

        public override Task SendAsync(
            ArraySegment<byte> buffer,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken)
            => SendAsync((ReadOnlyMemory<byte>)buffer.AsMemory(), messageType, endOfMessage, cancellationToken).AsTask();

        public override void Abort() => AbortCount++;

        public override Task CloseAsync(WebSocketCloseStatus s, string? d, CancellationToken t)
        {
            CloseCalls++;

            if (ThrowOnClose)
            {
                throw new WebSocketException(WebSocketError.Faulted);
            }

            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus s, string? d, CancellationToken t) => Task.CompletedTask;

        public override void Dispose() { }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> b, CancellationToken t)
            => throw new NotSupportedException();

        public override WebSocketCloseStatus? CloseStatus => null;

        public override string? CloseStatusDescription => null;

        public override string? SubProtocol => null;
    }

    private sealed class StubWriter : AbstractPacketWriter
    {
        public override void OnSerialize(INetworkPacketWriter writer) => writer.WriteByte(7);
    }

    private static NetworkClient CreateClient(
        WebSocket socket,
        IPacketCodecRegistry? registry = null,
        bool useWss = false)
    {
        if (registry == null)
        {
            var defaultRegistry = new Mock<IPacketCodecRegistry>();
            defaultRegistry.SetupGet(x => x.Default).Returns(TestPacketCodec.Instance);
            registry = defaultRegistry.Object;
        }

        return new NetworkClient(
            NullLogger<NetworkClient>.Instance,
            registry,
            Microsoft.Extensions.Options.Options.Create(new Ada.Networking.Options.NetworkOptions { UseWss = useWss }),
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

    private static async Task WaitForFramesAsync(FakeWebSocket socket, int count)
    {
        for (var i = 0; i < 200 && socket.Frames.Count < count; i++)
        {
            await Task.Delay(10);
        }
    }

    [Test]
    public void EncryptionEnabled_PlaintextTransport_StaysFalseAfterHandshake()
    {
        var client = CreateClient(new FakeWebSocket());

        Assert.That(client.EncryptionEnabled, Is.False);

        client.EnableEncryption([1, 2, 3]);

        Assert.That(client.EncryptionEnabled, Is.False);
    }

    [Test]
    public void EncryptionEnabled_SecureTransport_IsTrue()
    {
        var client = CreateClient(new FakeWebSocket(), useWss: true);

        Assert.That(client.EncryptionEnabled, Is.True);
    }

    [Test]
    public async Task WriteToStreamAsync_AbstractWriter_SerializesThroughCodec()
    {
        short packetId = 42;
        var idMap = new Mock<IPacketIdMap>();
        idMap.Setup(m => m.TryGetOutgoingId(typeof(StubWriter), out packetId)).Returns(true);

        var codec = new Mock<IPacketCodec>();
        codec.SetupGet(c => c.IdMap).Returns(idMap.Object);
        codec.Setup(c => c.CreateWriter()).Returns(() => new NetworkPacketWriter());

        var registry = new Mock<IPacketCodecRegistry>();
        registry.SetupGet(r => r.Default).Returns(codec.Object);

        var socket = new FakeWebSocket();
        var client = CreateClient(socket, registry.Object);

        await client.WriteToStreamAsync(new StubWriter());
        await WaitForFramesAsync(socket, 1);

        Assert.That(socket.Frames, Has.Count.EqualTo(1));
        Assert.That(socket.Frames[0], Has.Length.EqualTo(7));
    }

    [Test]
    public async Task QueueOutbound_Overflow_AbortsConnectionOnce()
    {
        var socket = new FakeWebSocket();
        var client = CreateClient(socket);

        var huge = new Mock<INetworkPacketWriter>();
        huge.Setup(w => w.GetAllBytes()).Returns(new byte[9 * 1024 * 1024]);

        client.QueueOutbound(huge.Object);
        client.QueueOutbound(huge.Object);

        Assert.That(socket.AbortCount, Is.EqualTo(1));

        client.QueueOutbound(PacketOf(1));
        await client.FlushAsync();

        Assert.That(socket.Frames, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task QueueOutbound_AfterDispose_IsIgnored()
    {
        var socket = new FakeWebSocket();
        var client = CreateClient(socket);

        await client.DisposeAsync();

        client.QueueOutbound(PacketOf(1));
        await client.FlushAsync();

        Assert.That(socket.Frames, Is.Empty);
    }

    [Test]
    public async Task FlushAsync_SendThrows_ClearsOutboxAndRecovers()
    {
        var socket = new FakeWebSocket { ThrowOnSend = true };
        var client = CreateClient(socket);

        client.QueueOutbound(PacketOf(1));
        await client.FlushAsync();

        Assert.That(socket.Frames, Is.Empty);

        socket.ThrowOnSend = false;
        client.QueueOutbound(PacketOf(2));
        await client.FlushAsync();

        Assert.That(socket.Frames, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task FlushAsync_SocketNotOpen_DrainsWithoutSending()
    {
        var socket = new FakeWebSocket { CurrentState = WebSocketState.Closed };
        var client = CreateClient(socket);

        client.QueueOutbound(PacketOf(1));
        await client.FlushAsync();

        Assert.That(socket.Frames, Is.Empty);
    }

    [Test]
    public async Task DisposeAsync_CalledTwice_SecondCallReturnsImmediately()
    {
        var socket = new FakeWebSocket();
        var client = CreateClient(socket);

        await client.DisposeAsync();
        await client.DisposeAsync();

        Assert.That(socket.CloseCalls, Is.EqualTo(1));
    }

    [Test]
    public void DisposeAsync_CloseThrowsWebSocketException_IsSwallowed()
    {
        var socket = new FakeWebSocket { ThrowOnClose = true };
        var client = CreateClient(socket);
        Assert.Multiple(() =>
        {
            Assert.DoesNotThrowAsync(async () => await client.DisposeAsync());
            Assert.That(socket.CloseCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task DisposeAsync_SocketAlreadyClosed_SkipsClose()
    {
        var socket = new FakeWebSocket { CurrentState = WebSocketState.Aborted };
        var client = CreateClient(socket);

        await client.DisposeAsync();

        Assert.That(socket.CloseCalls, Is.Zero);
    }
}

[TestFixture]
public class NetworkClientRepositoryExtendedTests
{
    private static PlayerDto MakePlayerDto(long id) => new(
        id, "user" + id, "user" + id + "@test.com",
        new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        [], null, null, [], [], [], [], null, null,
        [], [], [], [], [], [], [], [], [], [], [], [], [], [], [], [], []);

    private static Mock<IPlayerLogic> MakePlayer(long id, List<PlayerFriendshipDto>? friendships = null)
    {
        var player = new Mock<IPlayerLogic>();
        player.SetupGet(p => p.Player).Returns(MakePlayerDto(id));
        player.Setup(p => p.GetMergedFriendships()).Returns(friendships ?? []);
        return player;
    }

    private static Mock<INetworkClient> MakeClient(Guid guid, IPlayerLogic? player, IRoomUser? roomUser)
    {
        var client = new Mock<INetworkClient>();
        client.SetupGet(c => c.Guid).Returns(guid);
        client.SetupGet(c => c.Player).Returns(player);
        client.SetupGet(c => c.RoomUser).Returns(roomUser);
        client.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);
        return client;
    }

    [Test]
    public async Task TryRemoveAsync_PlayerRemovalFails_NotifiesListenersAndReturnsFalse()
    {
        var guid = Guid.NewGuid();
        var player = MakePlayer(10);

        var roomUserRepository = new Mock<IRoomUserRepository>();
        roomUserRepository.Setup(r => r.TryRemoveAsync(10, true, true)).Returns(Task.CompletedTask);
        var room = new Mock<IRoomLogic>();
        room.SetupGet(r => r.UserRepository).Returns(roomUserRepository.Object);
        var roomUser = new Mock<IRoomUser>();
        roomUser.SetupGet(u => u.Room).Returns(room.Object);
        roomUser.SetupGet(u => u.Player).Returns(player.Object);

        var goodListener = new Mock<IPlayerSessionListener>();
        var badListener = new Mock<IPlayerSessionListener>();
        badListener.Setup(l => l.OnDisconnectedAsync(player.Object, roomUser.Object))
            .ThrowsAsync(new InvalidOperationException());

        var playerRepository = new Mock<IPlayerRepository>();
        playerRepository.Setup(r => r.TryRemovePlayerAsync(10)).ReturnsAsync(false);

        var repository = new NetworkClientRepository(
            NullLogger<NetworkClientRepository>.Instance,
            playerRepository.Object,
            Mock.Of<IPlayerPresenceStore>(),
            Mock.Of<IPlayerHelperService>(),
            Mock.Of<IMapper>(),
            [goodListener.Object, badListener.Object]);

        var client = MakeClient(guid, player.Object, roomUser.Object);
        repository.AddClient(guid, client.Object);

        var removed = await repository.TryRemoveAsync(guid);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.False);
            Assert.That(repository.Clients, Is.Empty);
        });
        goodListener.Verify(l => l.OnDisconnectedAsync(player.Object, roomUser.Object), Times.Once);
        badListener.Verify(l => l.OnDisconnectedAsync(player.Object, roomUser.Object), Times.Once);
        roomUserRepository.Verify(r => r.TryRemoveAsync(10, true, true), Times.Once);
        client.Verify(c => c.DisposeAsync(), Times.Never);
    }

    [Test]
    public async Task TryRemoveAsync_PlayerWithFriends_UpdatesFriendStatusAndDisposes()
    {
        var guid = Guid.NewGuid();
        var friendships = new List<PlayerFriendshipDto> { new() { Id = 1, OriginPlayerId = 10, TargetPlayerId = 20 } };
        var player = MakePlayer(10, friendships);

        var playerRepository = new Mock<IPlayerRepository>();
        playerRepository.Setup(r => r.TryRemovePlayerAsync(10)).ReturnsAsync(true);

        var helper = new Mock<IPlayerHelperService>();

        var repository = new NetworkClientRepository(
            NullLogger<NetworkClientRepository>.Instance,
            playerRepository.Object,
            Mock.Of<IPlayerPresenceStore>(),
            helper.Object,
            Mock.Of<IMapper>(),
            []);

        var client = MakeClient(guid, player.Object, null);
        repository.AddClient(guid, client.Object);

        var removed = await repository.TryRemoveAsync(guid);

        Assert.That(removed, Is.True);
        helper.Verify(
            h => h.UpdatePlayerStatusForFriendsAsync(
                player.Object, friendships, false, false, playerRepository.Object),
            Times.Once);
        client.Verify(c => c.DisposeAsync(), Times.Once);
    }

    [Test]
    public async Task TryRemoveAsync_PlayerWithoutFriends_SkipsFriendStatusUpdate()
    {
        var guid = Guid.NewGuid();
        var player = MakePlayer(10);

        var playerRepository = new Mock<IPlayerRepository>();
        playerRepository.Setup(r => r.TryRemovePlayerAsync(10)).ReturnsAsync(true);

        var helper = new Mock<IPlayerHelperService>();

        var repository = new NetworkClientRepository(
            NullLogger<NetworkClientRepository>.Instance,
            playerRepository.Object,
            Mock.Of<IPlayerPresenceStore>(),
            helper.Object,
            Mock.Of<IMapper>(),
            []);

        var client = MakeClient(guid, player.Object, null);
        repository.AddClient(guid, client.Object);

        var removed = await repository.TryRemoveAsync(guid);

        Assert.That(removed, Is.True);
        helper.Verify(
            h => h.UpdatePlayerStatusForFriendsAsync(
                It.IsAny<IPlayerLogic>(),
                It.IsAny<IEnumerable<PlayerFriendshipDto>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<IPlayerRepository>()),
            Times.Never);
    }

    [Test]
    public async Task DisposeAsync_FailingRemoval_LogsAndContinues()
    {
        var guid = Guid.NewGuid();
        var player = MakePlayer(10);

        var playerRepository = new Mock<IPlayerRepository>();
        playerRepository.Setup(r => r.TryRemovePlayerAsync(10)).ReturnsAsync(false);

        var repository = new NetworkClientRepository(
            NullLogger<NetworkClientRepository>.Instance,
            playerRepository.Object,
            Mock.Of<IPlayerPresenceStore>(),
            Mock.Of<IPlayerHelperService>(),
            Mock.Of<IMapper>(),
            []);

        var client = MakeClient(guid, player.Object, null);
        repository.AddClient(guid, client.Object);

        await repository.DisposeAsync();

        Assert.That(repository.Clients, Is.Empty);
    }
}
