namespace Ada.API.Interfaces.Networking.Client;

public interface INetworkClientRepository : IAsyncDisposable
{
    void AddClient(Guid guid, INetworkClient client);
    Task<bool> TryRemoveAsync(Guid guid);
    Task DisconnectIdleClientsAsync();
    INetworkClient? TryGetClientByGuid(Guid guid);
    ICollection<INetworkClient> Clients { get; }
}