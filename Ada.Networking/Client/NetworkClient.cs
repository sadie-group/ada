using System.Net;
using System.Net.WebSockets;
using Ada.API;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Client;
using Ada.Networking.Packets.Serialization;
using Microsoft.Extensions.Logging;

namespace Ada.Networking.Client;

public class NetworkClient(
    ILogger<NetworkClient> logger,
    IPAddress ipAddress,
    Guid guid,
    WebSocket webSocket)
    : INetworkClient
{
    public IPAddress IpAddress { get; set; } = ipAddress;
    public Guid Guid { get; set; } = guid;
    public WebSocket WebSocket { get; set; } = webSocket;

    public IPlayerLogic? Player { get; set; }
    public IRoomUser? RoomUser { get; set; }
    public string? MachineId { get; set; }
    public bool EncryptionEnabled { get; private set; }

    public void EnableEncryption(byte[] sharedKey)
    {
        EncryptionEnabled = true;
    }

    public DateTime LastPing { get; set; } = DateTime.Now;
    public DateTime LastPong { get; set; } = DateTime.Now;

    private readonly SemaphoreSlim _sendLock = new(1, 1);

    private readonly object _outboxLock = new();
    private readonly List<INetworkPacketWriter> _outbox = [];

    public async Task WriteToStreamAsync(AbstractPacketWriter writer)
    {
        var serializedObject = NetworkPacketWriterSerializer.Serialize(writer);
        await WriteToStreamAsync(serializedObject);
    }

    public async Task WriteToStreamAsync(INetworkPacketWriter writer)
    {
        try
        {
            await SendBytesAsync(writer.GetAllBytes());
        }
        catch (Exception e)
        {
            logger.LogError(e.ToString());
        }
    }

    public void QueueOutbound(INetworkPacketWriter writer)
    {
        lock (_outboxLock)
        {
            _outbox.Add(writer);
        }
    }

    public async Task FlushAsync()
    {
        INetworkPacketWriter[] batch;

        lock (_outboxLock)
        {
            if (_outbox.Count == 0)
            {
                return;
            }

            batch = _outbox.ToArray();
            _outbox.Clear();
        }

        var payload = batch.SelectMany(x => x.GetAllBytes()).ToArray();
        await SendBytesAsync(payload);
    }

    private async Task SendBytesAsync(ReadOnlyMemory<byte> bytes)
    {
        await _sendLock.WaitAsync();
        try
        {
            if (WebSocket.State is WebSocketState.Open)
            {
                await WebSocket.SendAsync(bytes, WebSocketMessageType.Binary, true, CancellationToken.None);
            }
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private bool _disposed;

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

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
        finally
        {
            _sendLock.Dispose();
        }
    }
}