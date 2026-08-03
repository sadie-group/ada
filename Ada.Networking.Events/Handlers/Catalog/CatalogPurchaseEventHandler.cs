using Ada.API.DTOs.Catalog.Pages;
using Ada.API.Interfaces.Game.Catalog;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Catalog;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Constants;
using Ada.Db;
using Ada.Db.Models.Catalog.Pages;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Catalog;

[PacketId(EventHandlerId.CatalogPurchase)]
public class CatalogPurchaseEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    ICatalogChargeService catalogChargeService,
    ICatalogFurniturePurchaseService furniturePurchaseService,
    ICatalogBotPurchaseService botPurchaseService,
    ICatalogTeleportPurchaseService teleportPurchaseService,
    ICatalogPurchaseConfirmationService purchaseConfirmationService,
    ICatalogVipPurchaseService vipPurchaseProcessor,
    IMapper mapper) : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    public int PageId { get; set; }
    public int ItemId { get; set; }
    public string? MetaData { get; set; }
    public int Amount { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        if (Amount is < 1 ||
            (DateTime.Now - player.State.LastPlayerSearch).TotalMilliseconds < CooldownIntervals.CatalogPurchase)
        {
            await purchaseConfirmationService.WriteFailureAsync(client);
            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var efPage = await dbContext
            .Set<CatalogPage>()
            .Include(x => x.Items)
            .ThenInclude(x => x.FurnitureItems)
            .FirstOrDefaultAsync(x => x.Id == PageId);

        var page = mapper.Map<CatalogPageDto>(efPage);

        if (page == null)
        {
            await purchaseConfirmationService.WriteFailureAsync(client);
            return;
        }

        if (page.Layout == CatalogPageLayout.VipBuy)
        {
            await vipPurchaseProcessor.ProcessAsync(client, ItemId);
            return;
        }

        var item = page.Items.FirstOrDefault(x => x.Id == ItemId);

        if (item == null)
        {
            await purchaseConfirmationService.WriteFailureAsync(client);
            return;
        }

        if (!catalogChargeService.HasRequiredMembership(client, item))
        {
            await purchaseConfirmationService.WriteUnavailableAsync(client);
            return;
        }

        if (!await catalogChargeService.TryChargeAsync(client, item, Amount))
        {
            return;
        }

        if (page.Layout == CatalogPageLayout.Bots &&
            item.Name?.Contains("bot_") == true &&
            !string.IsNullOrEmpty(item.MetaData))
        {
            await botPurchaseService.ProcessAsync(client, item);
            return;
        }

        if (item.FurnitureItems.Any(x => x.InteractionType == FurnitureItemInteractionType.Teleport))
        {
            await teleportPurchaseService.ProcessAsync(client, item, MetaData, Amount);
            return;
        }

        await furniturePurchaseService.ProcessAsync(client, item, MetaData, Amount);
    }
}
