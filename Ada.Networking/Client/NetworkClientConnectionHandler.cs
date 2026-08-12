using System.Net.WebSockets;
using System.Threading.Channels;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Packets;
using Ada.Networking.Packets;
using Microsoft.Extensions.Logging;

namespace Ada.Networking.Client;

public class NetworkClientConnectionHandler(
    ILogger<NetworkClientConnectionHandler> logger,
    INetworkClientRepository clientRepository,
    IWebSocketMessageReader webSocketMessageReader,
    PacketDispatcher packetDispatcher,
    IPacketRateThrottle packetRateThrottle,
    IClientDisposalService clientDisposalService,
    TimeSpan? queueWaitTimeout = null)
    : INetworkClientConnectionHandler
{
    private const int _queueCapacity = 256;

    private readonly TimeSpan _queueWaitTimeout = queueWaitTimeout ?? TimeSpan.FromSeconds(10);

    public async Task HandleClientAsync(INetworkClient client, CancellationToken ct)
    {
        clientRepository.AddClient(client.Guid, client);

        var queue = Channel.CreateBounded<INetworkPacket>(new BoundedChannelOptions(_queueCapacity)
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = false
        });

        var consumer = ConsumeAsync(client, queue.Reader);

        try
        {
            await ReadAsync(client, queue.Writer, ct);
        }
        finally
        {
            queue.Writer.TryComplete();

            await DrainAsync(client, consumer);

            packetRateThrottle.Forget(client.Guid);

            await clientDisposalService.HandleDisconnectAsync(client);
        }
    }

    private async Task ReadAsync(INetworkClient client, ChannelWriter<INetworkPacket> writer, CancellationToken ct)
    {
        var socket = client.WebSocket;

        while (!ct.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            var (buffer, length) = await webSocketMessageReader.ReadMessageAsync(socket, ct);

            if (length == 0)
            {
                PacketBufferPool.Return(buffer);
                continue;
            }

            if (!packetRateThrottle.TryConsume(client.Guid, client.IpAddress))
            {
                PacketBufferPool.Return(buffer);

                logger.LogWarning(
                    "Client {Guid} from {Ip} exceeded its packet budget; closing the connection",
                    client.Guid,
                    client.IpAddress);

                client.WebSocket.Abort();
                break;
            }

            INetworkPacket packet;

            try
            {
                packet = client.Codec.Decoder.Decode(client.Guid, buffer, length);
            }
            catch (MalformedPacketException e)
            {
                PacketBufferPool.Return(buffer);

                logger.LogWarning(
                    "Discarded malformed frame from {Ip}: {Message}", client.IpAddress, e.Message);

                continue;
            }

            if (writer.TryWrite(packet))
            {
                continue;
            }

            if (await TryWaitForRoomAsync(client, writer, packet, ct))
            {
                continue;
            }

            break;
        }
    }

    private async Task DrainAsync(INetworkClient client, Task consumer)
    {
        try
        {
            await consumer.WaitAsync(_queueWaitTimeout);
        }
        catch (TimeoutException)
        {
            logger.LogError(
                "Client {Guid} from {Ip} disconnected while a handler was still running after {TimeoutSeconds}s; " +
                "releasing the connection without waiting for it",
                client.Guid,
                client.IpAddress,
                _queueWaitTimeout.TotalSeconds);
        }
    }

    private async Task<bool> TryWaitForRoomAsync(
        INetworkClient client,
        ChannelWriter<INetworkPacket> writer,
        INetworkPacket packet,
        CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);

        timeout.CancelAfter(_queueWaitTimeout);

        try
        {
            await writer.WriteAsync(packet, timeout.Token);
            return true;
        }
        catch (Exception e) when (e is OperationCanceledException or ChannelClosedException)
        {
            PacketBufferPool.Release(packet);

            if (ct.IsCancellationRequested)
            {
                return false;
            }

            logger.LogWarning(
                "Client {Guid} from {Ip} filled its {Capacity}-packet queue for {TimeoutSeconds}s; " +
                "a handler is stuck, so the connection is being closed",
                client.Guid,
                client.IpAddress,
                _queueCapacity,
                _queueWaitTimeout.TotalSeconds);

            client.WebSocket.Abort();

            return false;
        }
    }

    private async Task ConsumeAsync(INetworkClient client, ChannelReader<INetworkPacket> reader)
    {
        try
        {
            while (await reader.WaitToReadAsync())
            {
                while (reader.TryRead(out var packet))
                {
                    await packetDispatcher.ProcessAsync(client, packet);
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Packet queue for client {Guid} faulted", client.Guid);
        }
        finally
        {
            while (reader.TryRead(out var packet))
            {
                PacketBufferPool.Release(packet);
            }
        }
    }
}
