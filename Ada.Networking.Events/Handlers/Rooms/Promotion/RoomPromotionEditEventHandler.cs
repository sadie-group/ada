using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Promotion;

[PacketId(EventHandlerId.RoomPromotionEdit)]
public class RoomPromotionEditEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository) : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    public int RoomId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public int CategoryId { get; init; }

    private const int _maxNameLength = 60;
    private const int _maxDescriptionLength = 240;

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        var playerId = player.Player.Id;
        var now = DateTime.UtcNow;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var promotion = await dbContext.RoomPromotions
            .FirstOrDefaultAsync(x => x.RoomId == RoomId && x.OwnerId == playerId && x.ExpiresAt > now);

        if (promotion == null)
        {
            return;
        }

        promotion.Name = RoomPromotionPurchaseEventHandler.Truncate(Name, _maxNameLength);
        promotion.Description = RoomPromotionPurchaseEventHandler.Truncate(Description, _maxDescriptionLength);
        promotion.CategoryId = CategoryId;

        await dbContext.SaveChangesAsync();

        await RoomPromotionPurchaseEventHandler.BroadcastAsync(
            roomRepository,
            promotion,
            player.Player.Username);
    }
}
