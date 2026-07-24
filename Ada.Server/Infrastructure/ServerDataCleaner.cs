using Ada.API.Interfaces.Server;
using Ada.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ada.Server.Infrastructure;

public class ServerDataCleaner(
    ILogger<ServerDataCleaner> logger,
    IDbContextFactory<AdaDbContext> factory)
    : IServerDataCleaner
{
    public async Task CleanAsync(CancellationToken token)
    {
        await using var context = await factory.CreateDbContextAsync(token);

        logger.LogInformation("Cleaning up data...");

        await context.PlayerData
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsOnline, false), token);
    }
}