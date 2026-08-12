using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Constants;
using Ada.Db;
using Ada.Db.Models.Rooms;
using Ada.Networking.Writers.Navigator;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Navigator;

[PacketId(EventHandlerId.NavigatorTags)]
public class NavigatorTagsEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory) : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    public async Task HandleAsync(INetworkClient client)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var tags = await dbContext.Set<RoomTag>()
            .AsNoTracking()
            .GroupBy(x => x.Name)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(SearchLimits.NavigatorTags)
            .ToListAsync();

        await client.WriteToStreamAsync(new NavigatorTagsWriter
        {
            Tags = tags.ToDictionary(x => x.Name, x => x.Count)
        });
    }
}
