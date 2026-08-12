using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Players;
using Ada.Networking.Writers.Players.Navigator;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Navigator;

[PacketId(EventHandlerId.NavigatorAddSavedSearch)]
public class NavigatorAddSavedSearchEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory) : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    private const int _maxSavedSearches = 20;
    private const int _maxFieldLength = 64;

    public string? SearchCode { get; set; }
    public string? Filter { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        var searches = player.Player.SavedSearches;

        if (searches.Count >= _maxSavedSearches)
        {
            return;
        }

        var search = Truncate(SearchCode);
        var filter = Truncate(Filter);

        if (searches.Any(x => x.Search == search && x.Filter == filter))
        {
            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var entity = new PlayerSavedSearch
        {
            PlayerId = player.Player.Id,
            Search = search,
            Filter = filter
        };

        dbContext.Set<PlayerSavedSearch>().Add(entity);
        await dbContext.SaveChangesAsync();

        searches.Add(new PlayerSavedSearchDto
        {
            Id = entity.Id,
            PlayerId = player.Player.Id,
            Search = search,
            Filter = filter
        });

        await client.WriteToStreamAsync(new PlayerSavedSearchesWriter
        {
            Searches = searches
        });
    }

    private static string Truncate(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();

        return trimmed.Length > _maxFieldLength ? trimmed[.._maxFieldLength] : trimmed;
    }
}
