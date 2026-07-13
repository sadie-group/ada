using Microsoft.Extensions.Logging;
using Ada.API.Interfaces.Game.Catalog;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Server;
using Ada.API.Interfaces.Server.Tasks;
using IServer = Ada.API.IServer;

namespace Ada.Server;

public class Server(
    ILogger<Server> logger,
    IServerMigrator migrator,
    IServerDataCleaner dataCleaner,
    IServerTaskWorker taskWorker,
    INetworkClientRepository networkClientRepository,
    ICatalogPageRepository catalogPageRepository) : IServer
{
    
    public async Task RunAsync(CancellationToken token)
    {
        await migrator.MigrateAsync(token);
        await dataCleaner.CleanAsync(token);
        await taskWorker.WorkAsync(token);
        await catalogPageRepository.LoadAsync();
    }

    public async ValueTask DisposeAsync()
    {
        logger.LogWarning("Server is shutting down...");
        await networkClientRepository.DisposeAsync();
    }
}