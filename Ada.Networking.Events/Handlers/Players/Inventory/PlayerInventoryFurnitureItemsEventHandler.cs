using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Extensions;
using Ada.Networking.Writers.Players.Inventory;

namespace Ada.Networking.Events.Handlers.Players.Inventory;

[PacketId(EventHandlerId.PlayerInventoryFurnitureItems)]
public class PlayerInventoryFurnitureItemsEventHandler : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        var furnitureItems = client
            .Player
            .Player.FurnitureItems
            .Where(x => x.PlacementData == null)
            .ToList();

        if (furnitureItems.Count == 0)
        {
            await client.WriteToStreamAsync(new PlayerInventoryFurnitureItemsWriter
            {
                Pages = 1,
                CurrentPage = 0,
                Items = []
            });
            return;
        }

        var page = 0;
        var pages = (furnitureItems.Count - 1) / 700 + 1;
        
        foreach (var batch in furnitureItems.Batch(700))
        {
            await client.WriteToStreamAsync(new PlayerInventoryFurnitureItemsWriter
            {
                Pages = pages,
                CurrentPage = page,
                Items = batch.ToList()
            });
            
            page++;
        }
    }
}