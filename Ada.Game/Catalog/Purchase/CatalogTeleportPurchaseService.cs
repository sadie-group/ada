using Ada.API.DTOs.Catalog.Items;
using Ada.API.DTOs.Furniture;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Catalog;
using Ada.API.Interfaces.Networking.Client;
using Ada.Db;
using Ada.Db.Models.Players.Furniture;
using Ada.Networking.Writers.Players;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Game.Catalog.Purchase;

public class CatalogTeleportPurchaseService(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    ICatalogPurchaseConfirmationService confirmationService,
    IMapper mapper) : ICatalogTeleportPurchaseService
{
    public async Task ProcessAsync(INetworkClient client, CatalogItemDto item, string? metaData, int amount)
    {
        var created = DateTime.Now;
        var furniture = mapper.Map<FurnitureItemDto>(item.FurnitureItems.First());

        var parent = CreateItem(client, furniture, metaData, created);
        var child = CreateItem(client, furniture, metaData, created);

        client.Player.Player.FurnitureItems.Add(parent);
        client.Player.Player.FurnitureItems.Add(child);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        dbContext.Entry(parent).State = EntityState.Added;
        dbContext.Entry(child).State = EntityState.Added;

        await dbContext.SaveChangesAsync();

        dbContext.PlayerFurnitureItemLinks.Add(new PlayerFurnitureItemLink
        {
            ParentId = parent.Id,
            ChildId = child.Id
        });

        await dbContext.SaveChangesAsync();

        await client.WriteToStreamAsync(new PlayerInventoryUnseenItemsWriter
        {
            Count = 2,
            Category = 1,
            FurnitureItems = [parent, child]
        });

        await confirmationService.ConfirmAsync(client, item, amount);
    }

    private static PlayerFurnitureItemDto CreateItem(
        INetworkClient client,
        FurnitureItemDto furniture,
        string? metaData,
        DateTime created)
    {
        return new PlayerFurnitureItemDto
        {
            PlayerId = client.Player.Player.Id,
            FurnitureItemId = furniture.Id,
            FurnitureItem = furniture,
            LimitedData = "1:1",
            MetaData = metaData ?? "",
            CreatedAt = created
        };
    }
}
