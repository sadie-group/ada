using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Game.Rooms.Services.Wired;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Enums.Game.Rooms.Furniture;
using Ada.Db;
using Ada.Db.Models.Players.Furniture;
using Microsoft.EntityFrameworkCore;

namespace Ada.Game.Rooms.Services;

public class RoomWiredService(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomFurnitureItemHelperService furnitureItemHelperService,
    IEnumerable<IWiredEffectStrategy> effectStrategies,
    IEnumerable<IWiredConditionStrategy> conditionStrategies,
    IWiredTimerService timerService) : IRoomWiredService
{
    private const int PulseMilliseconds = 500;

    private readonly Dictionary<string, IWiredEffectStrategy> _effectStrategies =
        effectStrategies.ToDictionary(x => x.InteractionType);

    private readonly Dictionary<string, IWiredConditionStrategy> _conditionStrategies =
        conditionStrategies.ToDictionary(x => x.InteractionType);

    private static readonly ConcurrentDictionary<int, DateTimeOffset> PeriodicLastRuns = new();

    private sealed class PeriodicTriggerCache
    {
        public int SourceCount = -1;
        public List<PlayerFurnitureItemPlacementDataDto> Triggers = [];
    }

    private static readonly ConditionalWeakTable<ICollection<PlayerFurnitureItemPlacementDataDto>, PeriodicTriggerCache>
        PeriodicTriggerCaches = new();

    // Interaction types never change after placement, so the cache only needs to
    // refresh when items are added to or removed from the room.
    private static List<PlayerFurnitureItemPlacementDataDto> GetPeriodicTriggers(
        ICollection<PlayerFurnitureItemPlacementDataDto> roomItems)
    {
        var cache = PeriodicTriggerCaches.GetValue(roomItems, static _ => new PeriodicTriggerCache());

        if (cache.SourceCount != roomItems.Count)
        {
            cache.Triggers = roomItems
                .Where(x => GetInteractionType(x) is
                    FurnitureItemInteractionType.WiredTriggerAtGivenTime or
                    FurnitureItemInteractionType.WiredTriggerPeriodically or
                    FurnitureItemInteractionType.WiredTriggerPeriodicallyLong)
                .ToList();
            cache.SourceCount = roomItems.Count;
        }

        return cache.Triggers;
    }

    public IEnumerable<PlayerFurnitureItemPlacementDataDto> GetTriggers(
        string interactionType,
        IEnumerable<PlayerFurnitureItemPlacementDataDto> roomItems,
        string requiredMessage = "",
        List<int>? requiredSelectedIds = null)
    {
        return roomItems.Where(x =>
            x.WiredData != null &&
            x.PlayerFurnitureItem.FurnitureItem.InteractionType == interactionType &&
            MatchesMessage(x.WiredData.Message, requiredMessage) &&
            (requiredSelectedIds == null ||
             x.WiredData.SelectedItems.Any(i => requiredSelectedIds.Contains(i.Id))));
    }

    private static bool MatchesMessage(string wiredMessage, string requiredMessage)
    {
        return string.IsNullOrWhiteSpace(requiredMessage) ||
               string.IsNullOrWhiteSpace(wiredMessage) ||
               requiredMessage.Contains(wiredMessage, StringComparison.OrdinalIgnoreCase);
    }

    public async Task RunTriggerForRoomAsync(IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto trigger,
        IRoomUser? userWhoTriggered)
    {
        var stack = GetWiredStackForTrigger(trigger, room.Room.FurnitureItems).ToList();

        var conditionsPass = stack
            .Where(x => IsCondition(GetInteractionType(x)))
            .All(condition =>
                !_conditionStrategies.TryGetValue(GetInteractionType(condition), out var strategy) ||
                strategy.IsSatisfied(room, condition, userWhoTriggered));

        if (!conditionsPass)
        {
            return;
        }

        _ = CycleInteractionStateAsync(room, trigger);

        foreach (var effect in stack.Where(x => IsEffect(GetInteractionType(x))))
        {
            await RunEffectForRoomAsync(room, effect, userWhoTriggered);
        }
    }

    public async Task RunPeriodicTriggersForRoomAsync(IRoomLogic room)
    {
        var periodicTriggers = GetPeriodicTriggers(room.Room.FurnitureItems);

        if (periodicTriggers.Count == 0)
        {
            return;
        }

        foreach (var trigger in periodicTriggers)
        {
            if (trigger.WiredData == null)
            {
                continue;
            }

            var interactionType = GetInteractionType(trigger);

            if (interactionType == FurnitureItemInteractionType.WiredTriggerAtGivenTime)
            {
                var dueAfter = TimeSpan.FromMilliseconds(Math.Max(1, trigger.WiredData.Delay) * PulseMilliseconds);

                if (timerService.GetElapsed(room.Room.Id) >= dueAfter &&
                    timerService.TryMarkFired(room.Room.Id, trigger.Id))
                {
                    await RunTriggerForRoomAsync(room, trigger, null);
                }

                continue;
            }

            var pulseLength = interactionType == FurnitureItemInteractionType.WiredTriggerPeriodicallyLong
                ? PulseMilliseconds * 10
                : PulseMilliseconds;

            var interval = TimeSpan.FromMilliseconds(Math.Max(1, trigger.WiredData.Delay) * pulseLength);
            var now = DateTimeOffset.UtcNow;
            var lastRun = PeriodicLastRuns.GetOrAdd(trigger.Id, now);

            if (now - lastRun < interval)
            {
                continue;
            }

            PeriodicLastRuns[trigger.Id] = now;
            await RunTriggerForRoomAsync(room, trigger, null);
        }
    }

    public IEnumerable<PlayerFurnitureItemPlacementDataDto> GetEffectsForTrigger(
        PlayerFurnitureItemPlacementDataDto trigger,
        IEnumerable<PlayerFurnitureItemPlacementDataDto> roomItems)
    {
        return GetWiredStackForTrigger(trigger, roomItems)
            .Where(x => IsEffect(GetInteractionType(x)));
    }

    private static IEnumerable<PlayerFurnitureItemPlacementDataDto> GetWiredStackForTrigger(
        PlayerFurnitureItemPlacementDataDto trigger,
        IEnumerable<PlayerFurnitureItemPlacementDataDto> roomItems)
    {
        var stack = roomItems
            .Where(x =>
                x.PositionX == trigger.PositionX &&
                x.PositionY == trigger.PositionY &&
                x.PositionZ > trigger.PositionZ)
            .OrderBy(x => x.PositionZ);

        foreach (var item in stack)
        {
            var interactionType = GetInteractionType(item);

            if (!string.IsNullOrEmpty(interactionType) &&
                !IsEffect(interactionType) &&
                !IsCondition(interactionType))
            {
                break;
            }

            yield return item;
        }
    }

    private static string GetInteractionType(PlayerFurnitureItemPlacementDataDto item)
    {
        return item.PlayerFurnitureItem.FurnitureItem.InteractionType ?? "";
    }

    private static bool IsEffect(string interactionType) => interactionType.Contains("_act_");
    private static bool IsCondition(string interactionType) => interactionType.Contains("_cnd_");

    private async Task RunEffectForRoomAsync(
        IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto effect,
        IRoomUser? userWhoTriggered)
    {
        if (effect.WiredData == null ||
            !_effectStrategies.TryGetValue(GetInteractionType(effect), out var strategy))
        {
            return;
        }

        var delay = effect.WiredData.Delay;

        if (delay > 0)
        {
            _ = RunEffectDelayedAsync(room, effect, userWhoTriggered, strategy, delay);
            return;
        }

        await strategy.ExecuteAsync(room, effect, userWhoTriggered);
        _ = CycleInteractionStateAsync(room, effect);
    }

    private async Task RunEffectDelayedAsync(
        IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto effect,
        IRoomUser? userWhoTriggered,
        IWiredEffectStrategy strategy,
        int delayInPulses)
    {
        await Task.Delay(delayInPulses * PulseMilliseconds);
        await strategy.ExecuteAsync(room, effect, userWhoTriggered);
        _ = CycleInteractionStateAsync(room, effect);
    }

    public int GetWiredCode(string interactionType)
    {
        return interactionType switch
        {
            FurnitureItemInteractionType.WiredTriggerSaysSomething => (int) WiredTriggerCode.AvatarSaysSomething,
            FurnitureItemInteractionType.WiredTriggerEnterRoom => (int) WiredTriggerCode.AvatarEntersRoom,
            FurnitureItemInteractionType.WiredTriggerUserWalksOnFurniture => (int) WiredTriggerCode.AvatarWalksOnFurniture,
            FurnitureItemInteractionType.WiredTriggerUserWalksOffFurniture => (int) WiredTriggerCode.AvatarWalksOffFurniture,
            FurnitureItemInteractionType.WiredTriggerFurnitureStateChanged => (int) WiredTriggerCode.ToggleFurniture,
            FurnitureItemInteractionType.WiredTriggerPeriodically => (int) WiredTriggerCode.ExecutePeriodically,
            FurnitureItemInteractionType.WiredTriggerPeriodicallyLong => (int) WiredTriggerCode.ExecutePeriodicallyLong,
            FurnitureItemInteractionType.WiredTriggerAtGivenTime => (int) WiredTriggerCode.ExecuteOnce,
            FurnitureItemInteractionType.WiredEffectShowMessage => (int) WiredEffectCode.ShowMessage,
            FurnitureItemInteractionType.WiredEffectKickUser => (int) WiredEffectCode.KickUser,
            FurnitureItemInteractionType.WiredEffectToggleFurnitureState => (int) WiredEffectCode.ToggleFurnitureState,
            FurnitureItemInteractionType.WiredEffectTeleportToFurniture => (int) WiredEffectCode.TeleportToFurniture,
            FurnitureItemInteractionType.WiredEffectResetTimers => (int) WiredEffectCode.TimerReset,
            FurnitureItemInteractionType.WiredEffectMoveRotateFurniture => (int) WiredEffectCode.MoveRotateFurniture,
            FurnitureItemInteractionType.WiredEffectMoveFurnitureToClosestUser => (int) WiredEffectCode.MoveFurnitureToClosestUser,
            FurnitureItemInteractionType.WiredEffectFleeFromClosestUser => (int) WiredEffectCode.FleeFromClosestUser,
            FurnitureItemInteractionType.WiredEffectChangeFurnitureDirection => (int) WiredEffectCode.ChangeFurnitureDirection,
            FurnitureItemInteractionType.WiredEffectCallAnotherStack => (int) WiredEffectCode.CallAnotherStack,
            FurnitureItemInteractionType.WiredEffectMuteTriggerer => (int) WiredEffectCode.MuteTriggerer,
            FurnitureItemInteractionType.WiredConditionFurnitureHasUsers => (int) WiredConditionCode.FurnitureHasUsers,
            FurnitureItemInteractionType.WiredConditionNotFurnitureHasUsers => (int) WiredConditionCode.NotFurnitureHasUsers,
            FurnitureItemInteractionType.WiredConditionTriggererOnFurniture => (int) WiredConditionCode.TriggererOnFurniture,
            FurnitureItemInteractionType.WiredConditionNotTriggererOnFurniture => (int) WiredConditionCode.NotTriggererOnFurniture,
            FurnitureItemInteractionType.WiredConditionUserCountInRoom => (int) WiredConditionCode.UserCountInRoom,
            FurnitureItemInteractionType.WiredConditionNotUserCountInRoom => (int) WiredConditionCode.NotUserCountInRoom,
            FurnitureItemInteractionType.WiredConditionTimeElapsedMore => (int) WiredConditionCode.TimeElapsedMore,
            FurnitureItemInteractionType.WiredConditionTimeElapsedLess => (int) WiredConditionCode.TimeElapsedLess,
            FurnitureItemInteractionType.WiredConditionFurnitureHasFurniture => (int) WiredConditionCode.FurnitureHasFurniture,
            FurnitureItemInteractionType.WiredConditionNotFurnitureHasFurniture => (int) WiredConditionCode.NotFurnitureHasFurniture,
            FurnitureItemInteractionType.WiredConditionTriggererWearsBadge => (int) WiredConditionCode.TriggererWearsBadge,
            FurnitureItemInteractionType.WiredConditionNotTriggererWearsBadge => (int) WiredConditionCode.NotTriggererWearsBadge,
            FurnitureItemInteractionType.WiredConditionTriggererWearsEffect => (int) WiredConditionCode.TriggererWearsEffect,
            FurnitureItemInteractionType.WiredConditionNotTriggererWearsEffect => (int) WiredConditionCode.NotTriggererWearsEffect,
            FurnitureItemInteractionType.WiredConditionTriggererHasHandItem => (int) WiredConditionCode.TriggererHasHandItem,
            FurnitureItemInteractionType.WiredConditionDateRangeActive => (int) WiredConditionCode.DateRangeActive,
            _ => throw new ArgumentException($"Couldn't match interaction type '{interactionType}' to a trigger layout.")
        };
    }

    public async Task SaveSettingsAsync(
        PlayerFurnitureItemPlacementDataDto placementData,
        PlayerFurnitureItemWiredDataDto wiredData)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        await dbContext.Set<PlayerFurnitureItemWiredData>()
            .Where(x => x.PlayerFurnitureItemPlacementDataId == placementData.Id)
            .ExecuteDeleteAsync();

        var placementEntity = await dbContext.RoomFurnitureItems
            .FirstAsync(x => x.Id == placementData.Id);

        var selectedIds = wiredData.SelectedItems.Select(x => x.Id).ToList();
        var selectedEntities = selectedIds.Count > 0
            ? await dbContext.RoomFurnitureItems.Where(x => selectedIds.Contains(x.Id)).ToListAsync()
            : [];

        dbContext.Add(new PlayerFurnitureItemWiredData
        {
            PlayerFurnitureItemPlacementDataId = placementEntity.Id,
            PlacementData = placementEntity,
            SelectedItems = selectedEntities,
            Message = wiredData.Message,
            IntParameters = wiredData.IntParameters,
            Delay = wiredData.Delay
        });

        await dbContext.SaveChangesAsync();

        placementData.WiredData = wiredData;
    }

    private async Task CycleInteractionStateAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item)
    {
        await furnitureItemHelperService.UpdateMetaDataForItemAsync(room, item, "1");
        await Task.Delay(500);
        await furnitureItemHelperService.UpdateMetaDataForItemAsync(room, item, "0");
    }
}