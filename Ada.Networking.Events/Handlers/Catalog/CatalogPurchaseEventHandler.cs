using Ada.API.Interfaces.Game.Catalog;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Catalog;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Constants;
using Microsoft.Extensions.Logging;

namespace Ada.Networking.Events.Handlers.Catalog;

[PacketId(EventHandlerId.CatalogPurchase)]
public class CatalogPurchaseEventHandler(
    ICatalogPageRepository pageRepository,
    ICatalogChargeService catalogChargeService,
    ICatalogFurniturePurchaseService furniturePurchaseService,
    ICatalogBotPurchaseService botPurchaseService,
    ICatalogTeleportPurchaseService teleportPurchaseService,
    ICatalogPurchaseConfirmationService purchaseConfirmationService,
    ICatalogVipPurchaseService vipPurchaseProcessor,
    ILogger<CatalogPurchaseEventHandler> logger) : INetworkPacketEventHandler, IRunsOutsideRoomLock
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

        if (!PurchaseLimits.IsValidAmount(Amount) ||
            (DateTime.Now - player.State.LastCatalogPurchase).TotalMilliseconds < CooldownIntervals.CatalogPurchase)
        {
            await purchaseConfirmationService.WriteFailureAsync(client);
            return;
        }

        player.State.LastCatalogPurchase = DateTime.Now;

        var page = pageRepository.Pages.FirstOrDefault(x => x.Id == PageId);

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

        var isBotPurchase = page.Layout == CatalogPageLayout.Bots &&
                            item.Name?.Contains("bot_") == true &&
                            !string.IsNullOrEmpty(item.MetaData);

        var isTeleportPurchase =
            item.FurnitureItems.Any(x => x.InteractionType == FurnitureItemInteractionType.Teleport);

        var chargedAmount = isBotPurchase || isTeleportPurchase ? 1 : Amount;

        if (!await catalogChargeService.TryChargeAsync(client, item, chargedAmount))
        {
            await purchaseConfirmationService.WriteFailureAsync(client);
            return;
        }

        try
        {
            if (isBotPurchase)
            {
                await botPurchaseService.ProcessAsync(client, item);
                return;
            }

            if (isTeleportPurchase)
            {
                await teleportPurchaseService.ProcessAsync(client, item, MetaData, chargedAmount);
                return;
            }

            await furniturePurchaseService.ProcessAsync(client, item, MetaData, Amount);
        }
        catch (Exception e)
        {
            logger.LogError(e,
                "Catalog purchase delivery failed for player {PlayerId}, item {ItemId}; refunding",
                player.Player.Id, item.Id);

            await catalogChargeService.RefundAsync(client, item, chargedAmount);
            await purchaseConfirmationService.WriteFailureAsync(client);
        }
    }
}
