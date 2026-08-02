using Ada.API.DTOs.Catalog.Items;
using Ada.API.Interfaces.Game.Catalog;
using Ada.API.Interfaces.Networking.Client;
using Ada.Core.Players;
using Ada.Db;
using Ada.Networking.Writers.Players.Purse;
using Microsoft.EntityFrameworkCore;

namespace Ada.Game.Catalog.Purchase;

public class CatalogChargeService(IDbContextFactory<AdaDbContext> dbContextFactory) : ICatalogChargeService
{
    public bool HasRequiredMembership(INetworkClient client, CatalogItemDto item)
    {
        if (!item.RequiresClubMembership)
        {
            return true;
        }

        return client.Player?.Player.Subscriptions
            .FirstOrDefault(x => x.Subscription?.Name == "HABBO_CLUB") != null;
    }

    public async Task<bool> TryChargeAsync(INetworkClient client, CatalogItemDto item, int amount)
    {
        var costCredits = item.CostCredits * amount;
        var costPoints = item.CostPoints * amount;
        var player = client.Player;
        var data = player?.Player.Data;

        if (player == null || data == null)
        {
            return false;
        }

        if (data.CreditBalance < costCredits ||
            (item.CostPointsType == 0 && data.PixelBalance < costPoints) ||
            (item.CostPointsType != 0 && data.SeasonalBalance < costPoints))
        {
            return false;
        }

        if (costCredits > 0)
        {
            data.CreditBalance -= costCredits;

            await client.WriteToStreamAsync(new PlayerCreditsBalanceWriter
            {
                Credits = data.CreditBalance
            });
        }

        if (costPoints > 0)
        {
            if (item.CostPointsType == 0)
            {
                data.PixelBalance -= costPoints;
            }
            else
            {
                data.SeasonalBalance -= costPoints;
            }

            await client.WriteToStreamAsync(new PlayerActivityPointsBalanceWriter
            {
                Currencies = PlayerCurrencyMapper.FromBalances(
                    data.PixelBalance,
                    data.SeasonalBalance,
                    data.GotwPoints)
            });
        }

        if (costCredits <= 0 && costPoints <= 0)
        {
            return true;
        }

        var playerId = player.Player.Id;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        await dbContext.PlayerData
            .Where(x => x.PlayerId == playerId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.CreditBalance, data.CreditBalance)
                .SetProperty(x => x.PixelBalance, data.PixelBalance)
                .SetProperty(x => x.SeasonalBalance, data.SeasonalBalance)
                .SetProperty(x => x.GotwPoints, data.GotwPoints));

        return true;
    }
}
