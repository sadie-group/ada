using Ada.API.DTOs.Catalog.Pages;
using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Catalog.Pages;
using Ada.Networking.Writers.Players.Other;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Club;

[PacketId(EventHandlerId.HabboClubGifts)]
public class HabboClubGiftsEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IMapper mapper) : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    public async Task HandleAsync(INetworkClient client)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        var clubGiftPage = await dbContext
            .Set<CatalogPage>()
            .IgnoreAutoIncludes()
            .FirstOrDefaultAsync(x => x.Layout == "club_gift");

        var daysAsClub = CalculateDaysAsClub(client.Player.Player.Subscriptions);
        var daysTillNextClubGift = daysAsClub * 86400 / 2678400 * 2678400 - daysAsClub * 86400;
        var unclaimedGifts = daysAsClub * 86400 / 2678400 * 2678400 - daysAsClub * 86400; 
        
        await client.WriteToStreamAsync(new HabboClubGiftsWriter
        {
            DaysTillNext = daysTillNextClubGift,
            UnclaimedGifts = unclaimedGifts,
            DaysAsClub = daysAsClub,
            ClubGiftPage = mapper.Map<CatalogPageDto>(clubGiftPage)
        });
    }

    private static int CalculateDaysAsClub(ICollection<PlayerSubscriptionDto> subscriptions)
    {
        var days = 0;

        foreach (var subscription in subscriptions)
        {
            if (subscription.ExpiresAt >= DateTime.Now)
            {
                days += (int) (subscription.ExpiresAt - subscription.CreatedAt).TotalDays;
            }
            else
            {
                days += (int) (DateTime.Now - subscription.CreatedAt).TotalDays;
            }
        }

        return days;
    }
}