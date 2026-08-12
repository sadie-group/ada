using Ada.API.Interfaces.Game.Players;
using Microsoft.EntityFrameworkCore;

namespace Ada.Db.Players;

public class PlayerPresenceStore(IDbContextFactory<AdaDbContext> dbContextFactory) : IPlayerPresenceStore
{
    public async Task SetOfflineAsync(long playerId)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            await dbContext.PlayerData
                .Where(x => x.PlayerId == playerId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsOnline, false));
        }
        catch (DbUpdateConcurrencyException)
        {
        }
    }
}
