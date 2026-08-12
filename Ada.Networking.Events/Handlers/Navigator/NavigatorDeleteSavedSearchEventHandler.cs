using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Players;
using Ada.Networking.Writers.Players.Navigator;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Navigator;

[PacketId(EventHandlerId.NavigatorDeleteSavedSearch)]
public class NavigatorDeleteSavedSearchEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory) : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    public int SearchId { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        var searches = player.Player.SavedSearches;
        var existing = searches.FirstOrDefault(x => x.Id == SearchId);

        if (existing == null)
        {
            return;
        }

        var playerId = player.Player.Id;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        await dbContext.Set<PlayerSavedSearch>()
            .Where(x => x.Id == SearchId && x.PlayerId == playerId)
            .ExecuteDeleteAsync();

        searches.Remove(existing);

        await client.WriteToStreamAsync(new PlayerSavedSearchesWriter
        {
            Searches = searches
        });
    }
}
