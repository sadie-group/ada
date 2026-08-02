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

        var parentEntity = mapper.Map<PlayerFurnitureItem>(CreateItem(client, furniture, metaData, created));
        var childEntity = mapper.Map<PlayerFurnitureItem>(CreateItem(client, furniture, metaData, created));

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        // Attach the roots only; the mapped FurnitureItem navigations already exist in the database.
        dbContext.Entry(parentEntity).State = EntityState.Added;
        dbContext.Entry(childEntity).State = EntityState.Added;

        await dbContext.SaveChangesAsync();

        dbContext.PlayerFurnitureItemLinks.Add(new PlayerFurnitureItemLink
        {
            ParentId = parentEntity.Id,
            ChildId = childEntity.Id
        });

        await dbContext.SaveChangesAsync();

        var parent = mapper.Map<PlayerFurnitureItemDto>(parentEntity);
        var child = mapper.Map<PlayerFurnitureItemDto>(childEntity);

        client.Player!.Player.FurnitureItems.Add(parent);
        client.Player.Player.FurnitureItems.Add(child);

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
            PlayerId = client.Player!.Player.Id,
            FurnitureItemId = furniture.Id,
            FurnitureItem = furniture,
            LimitedData = "1:1",
            MetaData = metaData ?? "",
            CreatedAt = created
        };
    }
}
