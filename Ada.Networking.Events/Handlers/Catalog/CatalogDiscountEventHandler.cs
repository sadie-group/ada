using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Catalog;

namespace Ada.Networking.Events.Handlers.Catalog;

[PacketId(EventHandlerId.CatalogDiscount)]
public class CatalogDiscountEventHandler : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        await client.WriteToStreamAsync(new CatalogBundleDiscountRuleSetWriter
        {
            MaxPurchaseSize = 0,
            BundleSize = 0,
            BundleDiscountSize = 0,
            BonusThreshold = 0,
            AdditionalBonusDiscountThresholdQuantities = 0
        });
    }
}