using System.Buffers;
using System.Net.WebSockets;
using Ada.API.Interfaces.Networking;

namespace Ada.Networking;

public class WebSocketMessageReader : IWebSocketMessageReader
{
    private const int _maxMessageBytes = 256 * 1024;

    public async ValueTask<(byte[] buffer, int length)> ReadMessageAsync(WebSocket socket, CancellationToken token)
    {
        var recv = ArrayPool<byte>.Shared.Rent(4096);
        var message = ArrayPool<byte>.Shared.Rent(4096);
        var len = 0;

        try
        {
            while (true)
            {
                var result = await socket.ReceiveAsync(recv, token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    ArrayPool<byte>.Shared.Return(message);
                    ArrayPool<byte>.Shared.Return(recv);

                    return ([], 0);
                }

                if (len + result.Count > _maxMessageBytes)
                {
                    throw new WebSocketException(WebSocketError.InvalidMessageType);
                }

                if (len + result.Count > message.Length)
                {
                    var newMessage = ArrayPool<byte>.Shared.Rent(message.Length * 2);
                    Buffer.BlockCopy(message, 0, newMessage, 0, len);
                    ArrayPool<byte>.Shared.Return(message);
                    message = newMessage;
                }

                Buffer.BlockCopy(recv, 0, message, len, result.Count);
                len += result.Count;

                if (result.EndOfMessage)
                {
                    break;
                }
            }

            ArrayPool<byte>.Shared.Return(recv);
            return (message, len);
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(recv);
            ArrayPool<byte>.Shared.Return(message);
            throw;
        }
    }
}
