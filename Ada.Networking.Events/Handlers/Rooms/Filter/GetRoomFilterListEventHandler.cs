using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Writers.Rooms.Filter;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Filter;

[PacketId(EventHandlerId.GetRoomFilterList)]
public class GetRoomFilterListEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository) : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        var room = roomRepository.TryGetRoomById(player.State.CurrentRoomId);

        if (room == null || room.Room.OwnerId != player.Player.Id)
        {
            return;
        }

        var roomId = room.Room.Id;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var words = await dbContext.RoomWordFilters
            .Where(x => x.RoomId == roomId)
            .OrderBy(x => x.Id)
            .Select(x => x.Word)
            .ToListAsync();

        await client.WriteToStreamAsync(new RoomFilterListWriter
        {
            Words = words
        });
    }
}
