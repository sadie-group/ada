using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Server.Tasks;

namespace Ada.Server.Tasks.Networking;

public class DisconnectIdleClientsTask(INetworkClientRepository clientRepository) : IServerTask
{
    public TimeSpan PeriodicInterval => TimeSpan.FromSeconds(20);
    public long LastExecutedTicks { get; set; }

    public async Task ExecuteAsync()
    {
        await clientRepository.DisconnectIdleClientsAsync();
    }
}