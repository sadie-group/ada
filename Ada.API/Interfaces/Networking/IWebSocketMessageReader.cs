using System.Net.WebSockets;

namespace Ada.API.Interfaces.Networking;

public interface IWebSocketMessageReader
{
    ValueTask<(byte[] buffer, int length)> ReadMessageAsync(WebSocket socket, CancellationToken token);
}