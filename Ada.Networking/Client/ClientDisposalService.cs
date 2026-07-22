using Ada.API.Interfaces.Networking.Client;

namespace Ada.Networking.Client;

public class ClientDisposalService(INetworkClientRepository clientRepository) : IClientDisposalService
{
    public async Task HandleDisconnectAsync(INetworkClient client)
    {
        await clientRepository.TryRemoveAsync(client.Guid);
        await client.DisposeAsync();
    }
}
