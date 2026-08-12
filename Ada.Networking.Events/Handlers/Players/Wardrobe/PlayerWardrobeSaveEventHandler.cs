using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Helpers;
using Ada.Db;
using Ada.Db.Models.Players;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Players.Wardrobe;

[PacketId(EventHandlerId.PlayerWardrobeSave)]
public class PlayerWardrobeSaveEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IMapper mapper) : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    private const int _maxWardrobeSlots = 10;

    public int SlotId { get; set; }
    public required string FigureCode { get; set; }
    public required string Gender { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        if (!AvatarHelpers.IsValidFigureCode(FigureCode) || SlotId is < 0 or >= _maxWardrobeSlots)
        {
            return;
        }

        var gender = Gender == "M" ? PlayerAvatarGender.Male : PlayerAvatarGender.Female;
        var playerId = player.Player.Id;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var existing = await dbContext.PlayerWardrobeItems
            .FirstOrDefaultAsync(x =>
                EF.Property<long>(x, "PlayerId") == playerId && x.SlotId == SlotId);

        if (existing != null)
        {
            dbContext.PlayerWardrobeItems.Remove(existing);
        }

        var wardrobeItem = new PlayerWardrobeItem
        {
            SlotId = SlotId,
            FigureCode = FigureCode,
            Gender = gender
        };

        dbContext.PlayerWardrobeItems.Add(wardrobeItem);
        dbContext.Entry(wardrobeItem).Property("PlayerId").CurrentValue = playerId;
        await dbContext.SaveChangesAsync();

        foreach (var stale in player.Player.WardrobeItems.Where(x => x.SlotId == SlotId).ToList())
        {
            player.Player.WardrobeItems.Remove(stale);
        }

        player.Player.WardrobeItems.Add(
            mapper.Map<PlayerWardrobeItemDto>(wardrobeItem));
    }
}
