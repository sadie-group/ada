using System.Buffers;
using System.Net;
using System.Net.WebSockets;
using Ada.API;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Packets;
using Ada.Networking.Options;
using Ada.Networking.Packets.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ada.Networking.Client;

public class NetworkClient(
    ILogger<NetworkClient> logger,
    IPacketCodecRegistry codecRegistry,
    IOptions<NetworkOptions> networkOptions,
    IPAddress ipAddress,
    Guid guid,
    WebSocket webSocket)
    : INetworkClient
{
    public IPAddress IpAddress { get; set; } = ipAddress;
    public Guid Guid { get; set; } = guid;
    public WebSocket WebSocket { get; set; } = webSocket;
    public IPacketCodec Codec { get; set; } = codecRegistry.Default;

    public IPlayerLogic? Player { get; set; }
    public IRoomUser? RoomUser { get; set; }
    public string? MachineId { get; set; }

    public bool EncryptionEnabled => networkOptions.Value.UseWss;

    public bool TryApplyNegotiatedKey(byte[] sharedKey) => false;

    public DateTime LastPing { get; set; } = DateTime.UtcNow;
    public DateTime LastPong { get; set; } = DateTime.UtcNow;

    private const int MaxOutboxBytes = 8 * 1024 * 1024;

    private readonly object _outboxLock = new();
    private readonly List<INetworkPacketWriter> _outbox = [];
    private int _outboxBytes;
    private bool _sendInFlight;
    private bool _overflowed;
    private bool _disposed;
    private Task _pumpTask = Task.CompletedTask;

    public Task WriteToStreamAsync(AbstractPacketWriter writer)
        => WriteToStreamAsync(NetworkPacketWriterSerializer.Serialize(writer, Codec));

    public Task WriteToStreamAsync(INetworkPacketWriter writer)
    {
        QueueOutbound(writer);
        _ = FlushAsync();

        return Task.CompletedTask;
    }

    public void QueueOutbound(INetworkPacketWriter writer)
    {
        lock (_outboxLock)
        {
            if (_disposed)
            {
                return;
            }

            var length = writer.FramedLength;

            if (_outboxBytes + length > MaxOutboxBytes)
            {
                if (!_overflowed)
                {
                    _overflowed = true;
                    logger.LogError("Outbox overflow for client {Guid}, aborting connection", Guid);
                    WebSocket.Abort();
                }

                return;
            }

            _outbox.Add(writer);
            _outboxBytes += length;
        }
    }

    public Task FlushAsync()
    {
        lock (_outboxLock)
        {
            if (_sendInFlight || _outbox.Count == 0 || _disposed)
            {
                return Task.CompletedTask;
            }

            _sendInFlight = true;

            return _pumpTask = PumpAsync();
        }
    }

    private async Task PumpAsync()
    {
        try
        {
            while (true)
            {
                INetworkPacketWriter[] batch;

                lock (_outboxLock)
                {
                    if (_outbox.Count == 0 || _disposed)
                    {
                        _sendInFlight = false;
                        return;
                    }

                    batch = _outbox.ToArray();
                    _outbox.Clear();
                    _outboxBytes = 0;
                }

                await SendBatchAsync(batch);
            }
        }
        catch (Exception e)
        {
            lock (_outboxLock)
            {
                _sendInFlight = false;
                _outbox.Clear();
                _outboxBytes = 0;
            }

            WebSocket.Abort();

            logger.LogError(e.ToString());
        }
    }

    private static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(30);

    private CancellationTokenSource _sendCts = new();

    private CancellationToken StartSendTimeout()
    {
        if (!_sendCts.TryReset())
        {
            _sendCts.Dispose();
            _sendCts = new CancellationTokenSource();
        }

        _sendCts.CancelAfter(SendTimeout);

        return _sendCts.Token;
    }

    private async Task SendBatchAsync(INetworkPacketWriter[] batch)
    {
        if (WebSocket.State is not WebSocketState.Open)
        {
            return;
        }

        var token = StartSendTimeout();

        if (batch.Length == 1)
        {
            var single = batch[0];
            var singleLength = single.FramedLength;
            var singleBuffer = ArrayPool<byte>.Shared.Rent(singleLength);

            try
            {
                single.WriteFramedTo(singleBuffer.AsSpan(0, singleLength));

                await WebSocket.SendAsync(
                    singleBuffer.AsMemory(0, singleLength),
                    WebSocketMessageType.Binary,
                    true,
                    token);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(singleBuffer);
            }

            return;
        }

        var totalLength = 0;

        foreach (var writer in batch)
        {
            totalLength += writer.FramedLength;
        }

        var payload = ArrayPool<byte>.Shared.Rent(totalLength);

        try
        {
            var offset = 0;

            foreach (var writer in batch)
            {
                var length = writer.FramedLength;
                writer.WriteFramedTo(payload.AsSpan(offset, length));
                offset += length;
            }

            await WebSocket.SendAsync(
                payload.AsMemory(0, totalLength),
                WebSocketMessageType.Binary,
                true,
                token);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(payload);
        }
    }

    public async ValueTask DisposeAsync()
    {
        Task pump;

        lock (_outboxLock)
        {
            if (_disposed)
            {
                return;
            }

            pump = _pumpTask;
        }

        try
        {
            await FlushAsync().WaitAsync(TimeSpan.FromSeconds(5));
            await pump.WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (Exception e) when (e is TimeoutException or WebSocketException or OperationCanceledException)
        {
        }

        lock (_outboxLock)
        {
            _disposed = true;
            _outbox.Clear();
            _outboxBytes = 0;
        }

        _sendCts.Dispose();

        try
        {
            if (WebSocket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await WebSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Closing",
                    CancellationToken.None
                );
            }
        }
        catch (WebSocketException) {}
    }
}
