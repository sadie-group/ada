using Ada.API.DTOs.Catalog.Items;
using Ada.API.DTOs.Furniture;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Catalog;
using Ada.API.Interfaces.Game.WordFilter;
using Ada.API.Interfaces.Networking.Client;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Enums.Game.WordFilter;
using Ada.Core.Shared.Constants;
using Ada.Db;
using Ada.Db.Models.Players.Furniture;
using Ada.Networking.Writers.Players;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ada.Game.Catalog.Purchase;

public class CatalogFurniturePurchaseService(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    ICatalogPurchaseConfirmationService confirmationService,
    IWordFilterService wordFilterService,
    IMapper mapper,
    ILogger<CatalogFurniturePurchaseService> logger) : ICatalogFurniturePurchaseService
{
    private const int _maxTrophyMessageLength = 300;
    private const int _maxMetaDataLength = 300;
    private const string _defaultLimitedData = "1:1";

    public async Task ProcessAsync(INetworkClient client, CatalogItemDto item, string? metaData, int amount)
    {
        if (!PurchaseLimits.IsValidAmount(amount))
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount,
                "Purchase amount is outside the permitted range");
        }

        var created = DateTime.Now;
        var furniture = mapper.Map<FurnitureItemDto>(item.FurnitureItems.First());

        metaData = furniture.InteractionType == FurnitureItemInteractionType.Trophy
            ? BuildTrophyEngraving(client.Player!.Player.Username, metaData ?? "")
            : SanitiseMetaData(metaData);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        PlayerFurnitureItemDto BuildDto(int id) => new()
        {
            Id = id,
            PlayerId = client.Player!.Player.Id,
            FurnitureItemId = furniture.Id,
            FurnitureItem = furniture,
            LimitedData = _defaultLimitedData,
            MetaData = metaData,
            CreatedAt = created
        };

        var entities = new List<PlayerFurnitureItem>(amount);

        for (var i = 0; i < amount; i++)
        {
            entities.Add(mapper.Map<PlayerFurnitureItem>(BuildDto(0)));
        }

        dbContext.PlayerFurnitureItems.AddRange(entities);

        await dbContext.SaveChangesAsync();

        var newItems = entities.Select(entity => BuildDto(entity.Id)).ToList();

        foreach (var newItem in newItems)
        {
            client.Player!.Player.FurnitureItems.Add(newItem);
        }

        try
        {
            await client.WriteToStreamAsync(new PlayerInventoryUnseenItemsWriter
            {
                Count = newItems.Count,
                Category = 1,
                FurnitureItems = newItems
            });

            await confirmationService.ConfirmAsync(client, item, amount);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to notify player {PlayerId} of delivered furniture purchase",
                client.Player!.Player.Id);
        }
    }

    private string BuildTrophyEngraving(string username, string message)
    {
        message = Sanitise(message.Replace("\t", ""), _maxTrophyMessageLength);

        return $"{username}\t{DateTime.Now:d-M-yyyy}\t{message}";
    }

    private string SanitiseMetaData(string? metaData) =>
        string.IsNullOrEmpty(metaData) ? "" : Sanitise(metaData, _maxMetaDataLength);

    private string Sanitise(string message, int maxLength)
    {
        if (message.Length > maxLength)
        {
            message = message[..maxLength];
        }

        return wordFilterService.Filter(message, WordFilterContext.Chat).FilteredText;
    }
}
