using System.Buffers;
using System.Net.WebSockets;
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
    IClientDisposalService clientDisposalService)
    : INetworkClientConnectionHandler
{
    private static void ReturnBuffer(byte[] buffer)
    {
        if (buffer.Length != 0)
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public async Task HandleClientAsync(INetworkClient client, CancellationToken ct)
    {
        clientRepository.AddClient(client.Guid, client);

        try
        {
            var socket = client.WebSocket;

            while (!ct.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                var (buffer, length) = await webSocketMessageReader.ReadMessageAsync(socket, ct);

                if (length == 0)
                {
                    ReturnBuffer(buffer);
                    continue;
                }

                if (!packetRateThrottle.TryConsume(client.Guid))
                {
                    ReturnBuffer(buffer);

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
                    ReturnBuffer(buffer);

                    logger.LogWarning(
                        "Discarded malformed frame from {Ip}: {Message}", client.IpAddress, e.Message);

                    continue;
                }

                await packetDispatcher.ProcessAsync(client, packet);
            }
        }
        finally
        {
            packetRateThrottle.Forget(client.Guid);

            await clientDisposalService.HandleDisconnectAsync(client);
        }
    }
}
