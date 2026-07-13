namespace Ada.API.Interfaces.Networking;

public interface INetworkListener : IAsyncDisposable
{
    void Bootstrap();
    Task ListenAsync();
}