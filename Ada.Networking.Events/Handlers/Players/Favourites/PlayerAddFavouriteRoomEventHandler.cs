using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Players;
using Ada.Networking.Writers.Players.Favourites;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Players.Favourites;

[PacketId(EventHandlerId.PlayerAddFavouriteRoom)]
public class PlayerAddFavouriteRoomEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory) : INetworkPacketEventHandler
{
    public int RoomId { get; init; }

    private const int _favouriteRoomLimit = 30;

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null || RoomId < 1)
        {
            return;
        }

        var playerId = player.Player.Id;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        if (!await dbContext.Rooms.AnyAsync(x => x.Id == RoomId))
        {
            return;
        }

        var existing = await dbContext.PlayerFavouriteRooms
            .CountAsync(x => x.PlayerId == playerId);

        if (existing >= _favouriteRoomLimit)
        {
            return;
        }

        if (await dbContext.PlayerFavouriteRooms.AnyAsync(x => x.PlayerId == playerId && x.RoomId == RoomId))
        {
            return;
        }

        dbContext.PlayerFavouriteRooms.Add(new PlayerFavouriteRoom
        {
            PlayerId = playerId,
            RoomId = RoomId,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        await client.WriteToStreamAsync(new PlayerFavouriteRoomUpdateWriter
        {
            RoomId = RoomId,
            Added = true
        });
    }
}
