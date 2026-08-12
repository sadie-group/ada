using Ada.API.DTOs.Catalog;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Catalog;
using Ada.Networking.Writers.Players.Other;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Club;

[PacketId(EventHandlerId.HabboClubData)]
public class PlayerClubOffersEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IMapper mapper) : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    public int WindowId { get; set; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null)
        {
            return;
        }

        var daysRemaining = 0;
        
        var clubSubscription = client
            .Player
            .Player.Subscriptions
            .FirstOrDefault(x => x.Subscription?.Name == "HABBO_CLUB");

        if (clubSubscription != null)
        {
            var daysTotal = (clubSubscription.ExpiresAt - clubSubscription.CreatedAt).TotalDays;
            var daysSinceStarted = (DateTimeOffset.UtcNow - clubSubscription.CreatedAt).TotalDays;

            daysRemaining = (int)(daysTotal - daysSinceStarted);
        }
        
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        var catalogClubOffers = await dbContext
            .Set<CatalogClubOffer>()
            .ToListAsync();
        
        await client.WriteToStreamAsync(new PlayerClubOffersWriter
        {
            Offers = mapper.Map<IReadOnlyCollection<CatalogClubOfferDto>>(catalogClubOffers),
            WindowId = WindowId,
            Unused = false,
            CanGift = false,
            RemainingDays = daysRemaining
        });
    }
}