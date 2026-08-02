using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;
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

        var wardrobeItem = new PlayerWardrobeItem
        {
            SlotId = SlotId,
            FigureCode = FigureCode,
            Gender = Gender == "M" ? PlayerAvatarGender.Male : PlayerAvatarGender.Female
        };
        
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.PlayerWardrobeItems.Add(wardrobeItem);
        dbContext.Entry(wardrobeItem).Property("PlayerId").CurrentValue = player.Player.Id;
        await dbContext.SaveChangesAsync();
            
        player.Player.WardrobeItems.Add(
            mapper.Map<PlayerWardrobeItemDto>(wardrobeItem));
    }
}