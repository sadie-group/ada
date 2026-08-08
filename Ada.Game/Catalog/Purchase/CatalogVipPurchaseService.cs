using Ada.API.DTOs.Catalog.Items;
using Ada.API.Interfaces.Game.Catalog;
using Ada.API.Interfaces.Networking.Client;
using Ada.Networking.Writers.Catalog;
using Ada.Networking.Writers.Players.Inventory;

namespace Ada.Game.Catalog.Purchase;

public class CatalogVipPurchaseService : ICatalogVipPurchaseService
{
    public async Task ProcessAsync(INetworkClient client, CatalogItemDto item)
    {
        if (client.Player == null)
        {
            return;
        }

        await client.WriteToStreamAsync(new CatalogPurchaseOkWriter
        {
            Id = item.Id,
            Name = item.Name,
            Rented = false,
            CostCredits = item.CostCredits,
            CostPoints = item.CostPoints,
            CostPointsType = item.CostPointsType,
            CanGift = false,
            FurnitureItems = [],
            Amount = 1,
            ClubLevel = 1,
            CanPurchaseBundles = false,
            Metadata = item.MetaData,
            IsLimited = false,
            LimitedItemSeriesSize = 0,
            AmountLeft = 0
        });

        await client.WriteToStreamAsync(new PlayerInventoryRefreshWriter());
    }
}
