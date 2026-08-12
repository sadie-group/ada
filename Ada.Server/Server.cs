using Ada.API.Interfaces.Game.Catalog;
using Ada.API.Interfaces.Game.Moderation;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Server;
using Ada.API.Interfaces.Server.Tasks;
using Microsoft.Extensions.Logging;
using IServer = Ada.API.IServer;

namespace Ada.Server;

public class Server(
    ILogger<Server> logger,
    IServerMigrator migrator,
    IServerDataCleaner dataCleaner,
    IServerTaskWorker taskWorker,
    INetworkClientRepository networkClientRepository,
    ICatalogPageRepository catalogPageRepository,
    IModerationTicketService moderationTicketService) : IServer
{
    
    public async Task RunAsync(CancellationToken token)
    {
        await migrator.MigrateAsync(token);

        await Task.WhenAll(
            dataCleaner.CleanAsync(token),
            catalogPageRepository.LoadAsync(),
            moderationTicketService.LoadAsync());

        await taskWorker.WorkAsync(token);
    }

    public async ValueTask DisposeAsync()
    {
        logger.LogWarning("Server is shutting down...");
        await networkClientRepository.DisposeAsync();
    }
}