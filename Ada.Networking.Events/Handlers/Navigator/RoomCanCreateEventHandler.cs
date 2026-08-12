using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Writers.Navigator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Ada.Networking.Events.Handlers.Navigator;

[PacketId(EventHandlerId.RoomCanCreate)]
public class RoomCanCreateEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IConfiguration config) : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        var limit = config.GetValue("RoomOptions:MaxRoomsPerPlayer", 50);
        var playerId = player.Player.Id;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var owned = await dbContext.Rooms.CountAsync(x => x.OwnerId == playerId);

        await client.WriteToStreamAsync(new RoomCanCreateWriter
        {
            AtLimit = owned >= limit,
            RoomLimit = limit
        });
    }
}
