using Ada.API.DTOs.Server;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Server.Tasks;
using Ada.Core.Players;
using Ada.Db;
using Ada.Db.Models.Server;
using Ada.Networking.Writers.Players.Purse;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Server.Tasks.Game.Players;

public class PlayerCurrencyRewardsTask(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    List<ServerPeriodicCurrencyReward> rewards,
    IPlayerRepository playerRepository,
    ServerSettings serverSettings,
    IRoomRepository roomRepository,
    IMapper mapper) : IServerTask
{
    private const int _retainedLogsPerType = 1;

    public TimeSpan PeriodicInterval => TimeSpan.FromSeconds(1);
    public long LastExecutedTicks { get; set; }

    private readonly Dictionary<int, DateTime> _lastProcessed = rewards
        .ToDictionary(k => k.Id, _ => DateTime.Now);

    public async Task ExecuteAsync()
    {
        var rewardsToCheck = serverSettings.FairCurrencyRewards
            ? rewards
            : rewards
                .Where(r => (DateTime.Now - _lastProcessed[r.Id]).TotalSeconds >= r.IntervalSeconds);

        foreach (var reward in rewardsToCheck)
        {
            await CheckRewardsForPlayersAsync(reward);
            _lastProcessed[reward.Id] = DateTime.Now;
        }
    }

    private async Task CheckRewardsForPlayersAsync(ServerPeriodicCurrencyReward reward)
    {
        var players = playerRepository.GetAll();
        var logs = new List<ServerPeriodicCurrencyRewardLogDto>();

        var rewardedPlayerIds = new List<long>();

        foreach (var player in players)
        {
            var failIdleCheck = reward.SkipIdle && IsIdleInRoom(player);

            var failRoomCheck = reward.SkipHotelView &&
                player.State.CurrentRoomId == 0;

            if ((serverSettings.FairCurrencyRewards &&
                 !player.DeservesReward(reward.Type, reward.IntervalSeconds)) ||
                failIdleCheck ||
                failRoomCheck)
            {
                continue;
            }

            await RewardPlayerAsync(player, reward);

            var log = new ServerPeriodicCurrencyRewardLogDto
            {
                PlayerId = player.Player.Id,
                Type = reward.Type,
                Amount = reward.Amount,
                CreatedAt = DateTime.Now
            };

            player.Player.RewardLogs.Add(log);
            TrimRewardLogs(player.Player.RewardLogs, reward.Type);

            logs.Add(log);
            rewardedPlayerIds.Add(player.Player.Id);
        }

        if (logs.Count == 0)
        {
            return;
        }

        var entityLogs = mapper.Map<List<ServerPeriodicCurrencyRewardLog>>(logs);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.ServerPeriodicCurrencyRewardLogs.AddRangeAsync(entityLogs);
        await dbContext.SaveChangesAsync();

        await PersistBalancesAsync(dbContext, reward, rewardedPlayerIds);
    }

    private static async Task PersistBalancesAsync(
        AdaDbContext dbContext,
        ServerPeriodicCurrencyReward reward,
        List<long> playerIds)
    {
        if (playerIds.Count == 0)
        {
            return;
        }

        var amount = reward.Amount;

        var query = dbContext.PlayerData.Where(x => playerIds.Contains(x.PlayerId));

        switch (reward.Type)
        {
            case "credits":
                await query.ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.CreditBalance, x => x.CreditBalance + amount));
                break;
            case "pixels":
                await query.ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.PixelBalance, x => x.PixelBalance + amount));
                break;
            case "seasonal":
                await query.ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.SeasonalBalance, x => x.SeasonalBalance + amount));
                break;
        }
    }

    private bool IsIdleInRoom(IPlayerLogic player)
    {
        var room = roomRepository.TryGetRoomById(player.State.CurrentRoomId);

        return room != null &&
               room.UserRepository.TryGetById(player.Player.Id, out var roomUser) &&
               roomUser is { IsIdle: true };
    }

    private static void TrimRewardLogs(ICollection<ServerPeriodicCurrencyRewardLogDto> logs, string? type)
    {
        var overflow = logs.Count(x => x.Type == type) - _retainedLogsPerType;

        if (overflow <= 0)
        {
            return;
        }

        foreach (var stale in logs
                     .Where(x => x.Type == type)
                     .OrderBy(x => x.CreatedAt)
                     .Take(overflow)
                     .ToList())
        {
            logs.Remove(stale);
        }
    }

    private static async Task RewardPlayerAsync(IPlayerLogic player, ServerPeriodicCurrencyReward reward)
    {
        var data = player.Player.Data;

        if (data == null)
        {
            return;
        }

        AbstractPacketWriter? writer = null;

        switch (reward.Type)
        {
            case "credits":
                data.CreditBalance += reward.Amount;

                writer = new PlayerCreditsBalanceWriter
                {
                    Credits = data.CreditBalance
                };
                break;
            case "pixels":
                data.PixelBalance += reward.Amount;

                writer = new PlayerActivityPointsBalanceWriter
                {
                    Currencies = PlayerCurrencyMapper.FromBalances(
    data.PixelBalance,
    data.SeasonalBalance,
    data.GotwPoints)
                };
                break;
            case "seasonal":
                data.SeasonalBalance += reward.Amount;

                writer = new PlayerActivityPointsBalanceWriter
                {
                    Currencies = PlayerCurrencyMapper.FromBalances(
                        data.PixelBalance,
                        data.SeasonalBalance,
                        data.GotwPoints)
                };
                break;
        }

        if (writer != null)
        {
            await player.NetworkObject!.WriteToStreamAsync(writer);
        }
    }
}
