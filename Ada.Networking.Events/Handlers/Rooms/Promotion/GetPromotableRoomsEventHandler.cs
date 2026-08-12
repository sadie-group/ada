using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Writers.Rooms.Promotion;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Promotion;

[PacketId(EventHandlerId.GetPromotableRooms)]
public class GetPromotableRoomsEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory) : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    private const int _maxRooms = 50;

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        var playerId = player.Player.Id;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var rooms = await dbContext.Rooms
            .Where(x => x.OwnerId == playerId)
            .OrderByDescending(x => x.Id)
            .Take(_maxRooms)
            .Select(x => new { x.Id, x.Name })
            .ToListAsync();

        await client.WriteToStreamAsync(new PromotableRoomsWriter
        {
            Rooms = rooms.Select(x => new KeyValuePair<int, string>(x.Id, x.Name)).ToList()
        });
    }
}
