using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Writers.Players.Favourites;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Players.Favourites;

[PacketId(EventHandlerId.PlayerDeleteFavouriteRoom)]
public class PlayerDeleteFavouriteRoomEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory) : INetworkPacketEventHandler
{
    public int RoomId { get; init; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        var playerId = player.Player.Id;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var removed = await dbContext.PlayerFavouriteRooms
            .Where(x => x.PlayerId == playerId && x.RoomId == RoomId)
            .ExecuteDeleteAsync();

        if (removed == 0)
        {
            return;
        }

        await client.WriteToStreamAsync(new PlayerFavouriteRoomUpdateWriter
        {
            RoomId = RoomId,
            Added = false
        });
    }
}
