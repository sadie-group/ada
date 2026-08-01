using System.Net.WebSockets;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Client;
using Ada.Networking.Packets;

namespace Ada.Networking.Client;

public class NetworkClientConnectionHandler(
    INetworkClientRepository clientRepository,
    IWebSocketMessageReader webSocketMessageReader,
    PacketDispatcher packetDispatcher,
    IClientDisposalService clientDisposalService)
    : INetworkClientConnectionHandler
{
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
                    continue;
                }

                var packet = client.Codec.Decoder.Decode(client.Guid, buffer, length);
                await packetDispatcher.ProcessAsync(client, packet);
            }
        }
        finally
        {
            await clientDisposalService.HandleDisconnectAsync(client);
        }
    }
}