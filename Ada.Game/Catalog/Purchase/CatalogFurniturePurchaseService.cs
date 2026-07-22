using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Ada.API.DTOs.Catalog.Items;
using Ada.API.DTOs.Furniture;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Catalog;
using Ada.API.Interfaces.Game.WordFilter;
using Ada.API.Interfaces.Networking.Client;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Enums.Game.WordFilter;
using Ada.Db;
using Ada.Db.Models.Players.Furniture;
using Ada.Networking.Writers.Players;

namespace Ada.Game.Catalog.Purchase;

public class CatalogFurniturePurchaseService(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    ICatalogPurchaseConfirmationService confirmationService,
    IWordFilterService wordFilterService,
    IMapper mapper) : ICatalogFurniturePurchaseService
{
    private const int MaxTrophyMessageLength = 300;

    public async Task ProcessAsync(INetworkClient client, CatalogItemDto item, string? metaData, int amount)
    {
        var created = DateTime.Now;
        var furniture = mapper.Map<FurnitureItemDto>(item.FurnitureItems.First());

        if (furniture.InteractionType == FurnitureItemInteractionType.Trophy)
        {
            metaData = BuildTrophyEngraving(client.Player!.Player.Username, metaData ?? "");
        }
        var newItems = new List<PlayerFurnitureItemDto>();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        for (var i = 0; i < amount; i++)
        {
            var dto = new PlayerFurnitureItemDto
            {
                PlayerId = client.Player.Player.Id,
                FurnitureItemId = furniture.Id,
                FurnitureItem = furniture,
                LimitedData = "1:1",
                MetaData = metaData ?? "",
                CreatedAt = created
            };

            client.Player.Player.FurnitureItems.Add(dto);
            dbContext.PlayerFurnitureItems.Add(mapper.Map<PlayerFurnitureItem>(dto));
            newItems.Add(dto);
        }

        await dbContext.SaveChangesAsync();

        await client.WriteToStreamAsync(new PlayerInventoryUnseenItemsWriter
        {
            Count = newItems.Count,
            Category = 1,
            FurnitureItems = newItems
        });

        await confirmationService.ConfirmAsync(client, item, amount);
    }

    /// <summary>
    /// Trophies store their engraving in metadata as "username\tdd-MM-yyyy\tmessage",
    /// which the client renders when the trophy is inspected.
    /// </summary>
    private string BuildTrophyEngraving(string username, string message)
    {
        message = message.Replace("\t", "");

        if (message.Length > MaxTrophyMessageLength)
        {
            message = message[..MaxTrophyMessageLength];
        }

        message = wordFilterService.Filter(message, WordFilterContext.Chat).FilteredText;

        return $"{username}\t{DateTime.Now:d-M-yyyy}\t{message}";
    }
}