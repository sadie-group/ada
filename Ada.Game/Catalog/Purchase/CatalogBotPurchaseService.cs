using Microsoft.EntityFrameworkCore;
using Ada.API.DTOs.Catalog.Items;
using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Catalog;
using Ada.API.Interfaces.Networking.Client;
using Ada.Core.Enums.Game.Players;
using Ada.Db;
using Ada.Networking.Writers.Players.Inventory;

namespace Ada.Game.Catalog.Purchase;

public class CatalogBotPurchaseService(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    ICatalogPurchaseConfirmationService confirmationService) : ICatalogBotPurchaseService
{
    public async Task ProcessAsync(INetworkClient client, CatalogItemDto item)
    {
        var data = item.MetaData;

        if (string.IsNullOrEmpty(data))
        {
            return;
        }

        var info = data.Split(";")
            .ToDictionary(x => x.Split(":")[0], x => x.Split(":")[1]);

        var bot = new PlayerBotDto
        {
            PlayerId = client.Player.Player.Id,
            Username = info["name"],
            FigureCode = info["figure"],
            Motto = info["motto"],
            Gender = info["gender"].ToUpper() == "M"
                ? PlayerAvatarGender.Male
                : PlayerAvatarGender.Female,
            CreatedAt = DateTime.Now
        };

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.Entry(bot).State = EntityState.Added;
        await dbContext.SaveChangesAsync();

        client.Player.Player.Bots.Add(bot);

        await client.WriteToStreamAsync(new PlayerInventoryAddBotWriter
        {
            Id = bot.Id,
            Username = bot.Username,
            Motto = bot.Motto,
            Gender = bot.Gender == PlayerAvatarGender.Male ? "m" : "f",
            FigureCode = bot.FigureCode,
            OpenInventory = true
        });

        await confirmationService.ConfirmAsync(client, item, 1);
    }
}