using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Server.Tasks;
using Ada.Networking.Writers.Players.Other;

namespace Ada.Server.Tasks.Networking;

public class PingClientsTask(INetworkClientRepository networkClientRepository) : IServerTask
{
    public TimeSpan PeriodicInterval => TimeSpan.FromSeconds(10);
    public long LastExecutedTicks { get; set; }
    
    public async Task ExecuteAsync()
    {
        foreach (var client in networkClientRepository.Clients)
        {
            await client.WriteToStreamAsync(new PlayerPingWriter());
            client.LastPing = DateTime.Now;
        }
    }
}