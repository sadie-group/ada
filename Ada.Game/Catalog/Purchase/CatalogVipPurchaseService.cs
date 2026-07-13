using Microsoft.EntityFrameworkCore;
using Ada.API.Interfaces.Networking.Client;
using Ada.Core.Enums.Game.Catalog;
using Ada.Db;
using Ada.Db.Models.Catalog.Items;
using Ada.Networking.Writers.Catalog;
using Ada.Networking.Writers.Players.Inventory;

namespace Ada.Game.Catalog.Purchase;

public class CatalogVipPurchaseService(
    IDbContextFactory<AdaDbContext> dbContextFactory) : ICatalogVipPurchaseService
{
    public async Task ProcessAsync(INetworkClient client, int itemId)
    {
        if (client.Player == null)
        {
            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var item = await dbContext
            .Set<CatalogItem>()
            .FirstOrDefaultAsync(x => x.Id == itemId);

        if (item == null)
        {
            await client.WriteToStreamAsync(new CatalogPurchaseFailedWriter
            {
                Error = (int)CatalogPurchaseError.Server
            });

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