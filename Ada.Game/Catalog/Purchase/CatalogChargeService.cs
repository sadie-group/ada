using Ada.API.DTOs.Catalog.Items;
using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Catalog;
using Ada.API.Interfaces.Networking.Client;
using Ada.Core.Players;
using Ada.Core.Shared.Constants;
using Ada.Db;
using Ada.Networking.Writers.Players.Purse;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ada.Game.Catalog.Purchase;

public class CatalogChargeService(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    ILogger<CatalogChargeService> logger) : ICatalogChargeService
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
        var player = client.Player;
        var data = player?.Player.Data;

        if (player == null || data == null)
        {
            return false;
        }

        if (!PurchaseLimits.IsValidAmount(amount) ||
            !TryGetCosts(item, amount, out var credits, out var pixels, out var seasonal))
        {
            return false;
        }

        if (credits <= 0 && pixels <= 0 && seasonal <= 0)
        {
            return true;
        }

        if (data.CreditBalance < credits ||
            data.PixelBalance < pixels ||
            data.SeasonalBalance < seasonal)
        {
            return false;
        }

        var playerId = player.Player.Id;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var updated = await dbContext.PlayerData
            .Where(x => x.PlayerId == playerId &&
                        x.CreditBalance >= credits &&
                        x.PixelBalance >= pixels &&
                        x.SeasonalBalance >= seasonal)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.CreditBalance, x => x.CreditBalance - credits)
                .SetProperty(x => x.PixelBalance, x => x.PixelBalance - pixels)
                .SetProperty(x => x.SeasonalBalance, x => x.SeasonalBalance - seasonal));

        if (updated == 0)
        {
            return false;
        }

        await SyncBalancesAsync(client, data, dbContext, playerId, credits != 0, pixels != 0 || seasonal != 0);
        return true;
    }

    public async Task RefundAsync(INetworkClient client, CatalogItemDto item, int amount)
    {
        var player = client.Player;
        var data = player?.Player.Data;

        if (player == null || data == null ||
            !PurchaseLimits.IsValidAmount(amount) ||
            !TryGetCosts(item, amount, out var credits, out var pixels, out var seasonal))
        {
            logger.LogError(
                "Cannot refund {Amount} of item {ItemId} for player {PlayerId}: the charge cannot be reconstructed",
                amount, item.Id, client.Player?.Player.Id);

            return;
        }

        if (credits <= 0 && pixels <= 0 && seasonal <= 0)
        {
            return;
        }

        var playerId = player.Player.Id;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        await dbContext.PlayerData
            .Where(x => x.PlayerId == playerId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.CreditBalance, x => x.CreditBalance + credits)
                .SetProperty(x => x.PixelBalance, x => x.PixelBalance + pixels)
                .SetProperty(x => x.SeasonalBalance, x => x.SeasonalBalance + seasonal));

        await SyncBalancesAsync(client, data, dbContext, playerId, credits != 0, pixels != 0 || seasonal != 0);
    }

    private static bool TryGetCosts(CatalogItemDto item, int amount, out int credits, out int pixels, out int seasonal)
    {
        credits = 0;
        pixels = 0;
        seasonal = 0;

        var costCredits = (long) item.CostCredits * amount;
        var costPoints = (long) item.CostPoints * amount;

        if (costCredits is < 0 or > int.MaxValue || costPoints is < 0 or > int.MaxValue)
        {
            return false;
        }

        credits = (int) costCredits;
        pixels = item.CostPointsType == 0 ? (int) costPoints : 0;
        seasonal = item.CostPointsType != 0 ? (int) costPoints : 0;
        return true;
    }

    private static async Task SyncBalancesAsync(
        INetworkClient client,
        PlayerDataDto data,
        AdaDbContext dbContext,
        long playerId,
        bool writeCredits,
        bool writePoints)
    {
        var balances = await dbContext.PlayerData
            .AsNoTracking()
            .Where(x => x.PlayerId == playerId)
            .Select(x => new
            {
                x.CreditBalance,
                x.PixelBalance,
                x.SeasonalBalance,
                x.GotwPoints
            })
            .FirstOrDefaultAsync();

        if (balances == null)
        {
            return;
        }

        data.CreditBalance = balances.CreditBalance;
        data.PixelBalance = balances.PixelBalance;
        data.SeasonalBalance = balances.SeasonalBalance;
        data.GotwPoints = balances.GotwPoints;

        if (writeCredits)
        {
            await client.WriteToStreamAsync(new PlayerCreditsBalanceWriter
            {
                Credits = data.CreditBalance
            });
        }

        if (writePoints)
        {
            await client.WriteToStreamAsync(new PlayerActivityPointsBalanceWriter
            {
                Currencies = PlayerCurrencyMapper.FromBalances(
                    data.PixelBalance,
                    data.SeasonalBalance,
                    data.GotwPoints)
            });
        }
    }
}
