using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Catalog;

namespace Ada.Networking.Events.Handlers.Catalog;

[PacketId(EventHandlerId.CatalogGiftConfig)]
public class CatalogGiftConfigEventHandler : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        await client.WriteToStreamAsync(new CatalogGiftWrappingConfigWriter
        {
            Enabled = true,
            Price = 3,
            GiftWrappers = [],
            BoxTypes = [],
            RibbonTypes = [],
            GiftFurniture = []
        });
    }
}