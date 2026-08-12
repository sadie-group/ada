using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Rooms;
using Ada.Networking.Writers.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Promotion;

[PacketId(EventHandlerId.RoomPromotionPurchase)]
public class RoomPromotionPurchaseEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository) : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    public int RoomId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public int CategoryId { get; init; }

    private const int _durationMinutes = 120;
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

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        if (!await dbContext.Rooms.AnyAsync(x => x.Id == RoomId && x.OwnerId == playerId))
        {
            return;
        }

        var now = DateTime.UtcNow;

        if (await dbContext.RoomPromotions.AnyAsync(x => x.RoomId == RoomId && x.ExpiresAt > now))
        {
            return;
        }

        var promotion = new RoomPromotion
        {
            RoomId = RoomId,
            OwnerId = playerId,
            Name = Truncate(Name, _maxNameLength),
            Description = Truncate(Description, _maxDescriptionLength),
            CategoryId = CategoryId,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(_durationMinutes)
        };

        dbContext.RoomPromotions.Add(promotion);

        await dbContext.SaveChangesAsync();

        await BroadcastAsync(roomRepository, promotion, player.Player.Username);
    }

    internal static string Truncate(string value, int maxLength)
    {
        var trimmed = value.Trim();

        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }

    internal static async Task BroadcastAsync(
        IRoomRepository roomRepository,
        RoomPromotion promotion,
        string ownerUsername)
    {
        var room = roomRepository.TryGetRoomById(promotion.RoomId);

        if (room == null)
        {
            return;
        }

        await room.BroadcastDataAsync(new RoomPromotionWriter
        {
            AdId = promotion.Id,
            OwnerId = promotion.OwnerId,
            OwnerUsername = ownerUsername,
            FlatId = promotion.RoomId,
            Type = 0,
            Name = promotion.Name,
            Description = promotion.Description,
            Unknown8 = 0,
            Unknown9 = 0,
            CategoryId = promotion.CategoryId
        });
    }
}
