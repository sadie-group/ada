using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Catalog;

namespace Ada.Networking.Events.Handlers.Catalog;

[PacketId(EventHandlerId.CatalogMarketplaceConfig)]
public class CatalogMarketplaceConfigEventHandler : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        await client.WriteToStreamAsync(new CatalogMarketplaceConfigWriter
        {
            Unknown = true,
            CommissionPercent = 1,
            Credits = 10,
            Advertisements = 5,
            MinPrice = 1,
            MaxPrice = 1000000,
            HoursInMarketplace = 48,
            DaysToDisplay = 7
        });
    }
}