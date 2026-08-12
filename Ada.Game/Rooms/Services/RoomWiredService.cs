using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Ada.API.Collections;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Game.Rooms.Services.Wired;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Enums.Game.Rooms.Furniture;
using Ada.Core.Shared.Extensions;
using Ada.Db;
using Ada.Db.Models.Players.Furniture;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ada.Game.Rooms.Services;

public class RoomWiredService(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomFurnitureItemHelperService furnitureItemHelperService,
    IRoomTileMapHelperService tileMapHelperService,
    IEnumerable<IWiredEffectStrategy> effectStrategies,
    IEnumerable<IWiredConditionStrategy> conditionStrategies,
    IWiredTimerService timerService,
    ILogger<RoomWiredService> logger) : IRoomWiredService
{
    private const int _pulseMilliseconds = 500;

    private const long _maxIntervalMilliseconds = 120L * _pulseMilliseconds * 10;

    private readonly Dictionary<string, IWiredEffectStrategy> _effectStrategies =
        effectStrategies.ToDictionary(x => x.InteractionType);

    private readonly Dictionary<string, IWiredConditionStrategy> _conditionStrategies =
        conditionStrategies.ToDictionary(x => x.InteractionType);

    private sealed class PeriodicTriggerSnapshot
    {
        public required int Revision { get; init; }
        public required List<PlayerFurnitureItemPlacementDataDto> Triggers { get; init; }
    }

    private sealed class PeriodicTriggerCache
    {
        public volatile PeriodicTriggerSnapshot? Current;

        public readonly ConcurrentDictionary<int, DateTimeOffset> LastRuns = new();
    }

    private static readonly ConditionalWeakTable<ICollection<PlayerFurnitureItemPlacementDataDto>, PeriodicTriggerCache>
        _periodicTriggerCaches = new();

    private static (PeriodicTriggerCache Cache, List<PlayerFurnitureItemPlacementDataDto> Triggers)
        GetPeriodicTriggers(ICollection<PlayerFurnitureItemPlacementDataDto> roomItems)
    {
        var cache = _periodicTriggerCaches.GetValue(roomItems, static _ => new PeriodicTriggerCache());

        var (revision, snapshot) = CollectionRevision.SnapshotOf(roomItems);
        var current = cache.Current;

        if (current != null && current.Revision == revision)
        {
            return (cache, current.Triggers);
        }

        var triggers = snapshot
            .Where(x => GetInteractionType(x) is
                FurnitureItemInteractionType.WiredTriggerAtGivenTime or
                FurnitureItemInteractionType.WiredTriggerPeriodically or
                FurnitureItemInteractionType.WiredTriggerPeriodicallyLong)
            .ToList();

        cache.Current = new PeriodicTriggerSnapshot
        {
            Revision = revision,
            Triggers = triggers
        };

        var triggerIds = triggers.Select(x => x.Id).ToHashSet();

        foreach (var staleId in cache.LastRuns.Keys.Where(id => !triggerIds.Contains(id)))
        {
            cache.LastRuns.TryRemove(staleId, out _);
        }

        return (cache, triggers);
    }

    private sealed class InteractionTypeIndex
    {
        public required int Revision { get; init; }

        public required IReadOnlyDictionary<string, IReadOnlyList<PlayerFurnitureItemPlacementDataDto>>
            ByInteractionType { get; init; }
    }

    private sealed class InteractionTypeIndexSlot
    {
        public volatile InteractionTypeIndex? Current;
    }

    private static readonly ConditionalWeakTable<ICollection<PlayerFurnitureItemPlacementDataDto>, InteractionTypeIndexSlot>
        _interactionTypeIndexes = new();

    private static IReadOnlyList<PlayerFurnitureItemPlacementDataDto> GetItemsByInteractionType(
        ICollection<PlayerFurnitureItemPlacementDataDto> roomItems,
        string interactionType)
    {
        var slot = _interactionTypeIndexes.GetValue(roomItems, static _ => new InteractionTypeIndexSlot());

        var (revision, snapshot) = CollectionRevision.SnapshotOf(roomItems);
        var index = slot.Current;

        if (index == null || index.Revision != revision)
        {
            var map = new Dictionary<string, List<PlayerFurnitureItemPlacementDataDto>>();

            foreach (var item in snapshot)
            {
                var type = GetInteractionType(item);

                if (string.IsNullOrEmpty(type))
                {
                    continue;
                }

                if (!map.TryGetValue(type, out var bucket))
                {
                    map[type] = bucket = [];
                }

                bucket.Add(item);
            }

            index = new InteractionTypeIndex
            {
                Revision = revision,
                ByInteractionType = map.ToDictionary(
                    x => x.Key,
                    x => (IReadOnlyList<PlayerFurnitureItemPlacementDataDto>) x.Value.ToArray())
            };

            slot.Current = index;
        }

        return index.ByInteractionType.TryGetValue(interactionType, out var result) ? result : [];
    }

    public bool HasTriggers(string interactionType, ICollection<PlayerFurnitureItemPlacementDataDto> roomItems)
    {
        var candidates = GetItemsByInteractionType(roomItems, interactionType);

        for (var i = 0; i < candidates.Count; i++)
        {
            if (candidates[i].WiredData != null)
            {
                return true;
            }
        }

        return false;
    }

    public IEnumerable<PlayerFurnitureItemPlacementDataDto> GetTriggers(
        string interactionType,
        IEnumerable<PlayerFurnitureItemPlacementDataDto> roomItems,
        string requiredMessage = "",
        List<int>? requiredSelectedIds = null)
    {
        var candidates = roomItems is ICollection<PlayerFurnitureItemPlacementDataDto> collection
            ? GetItemsByInteractionType(collection, interactionType)
            : roomItems.Where(x => x.PlayerFurnitureItem.FurnitureItem.InteractionType == interactionType);

        return candidates.Where(x =>
            x.WiredData != null &&
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

        RunDetached(() => CycleInteractionStateAsync(room, trigger), "wired trigger state cycle");

        foreach (var effect in stack.Where(x => IsEffect(GetInteractionType(x))))
        {
            await RunEffectForRoomAsync(room, effect, userWhoTriggered);
        }
    }

    public async Task RunPeriodicTriggersForRoomAsync(IRoomLogic room)
    {
        var (cache, periodicTriggers) = GetPeriodicTriggers(room.Room.FurnitureItems);

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
                var dueAfter = TimeSpan.FromMilliseconds(Math.Max(1, trigger.WiredData.Delay) * _pulseMilliseconds);

                if (timerService.GetElapsed(room.Room.Id) >= dueAfter &&
                    timerService.TryMarkFired(room.Room.Id, trigger.Id))
                {
                    await RunTriggerForRoomAsync(room, trigger, null);
                }

                continue;
            }

            var pulseLength = interactionType == FurnitureItemInteractionType.WiredTriggerPeriodicallyLong
                ? _pulseMilliseconds * 10
                : _pulseMilliseconds;

            var interval = TimeSpan.FromMilliseconds(
                Math.Clamp((long) Math.Max(1, trigger.WiredData.Delay) * pulseLength, pulseLength, _maxIntervalMilliseconds));
            var now = DateTimeOffset.UtcNow;

            if (!cache.LastRuns.TryGetValue(trigger.Id, out var lastRun))
            {
                cache.LastRuns[trigger.Id] = now;
                continue;
            }

            if (now - lastRun < interval)
            {
                continue;
            }

            cache.LastRuns[trigger.Id] = now;
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

    private IEnumerable<PlayerFurnitureItemPlacementDataDto> GetWiredStackForTrigger(
        PlayerFurnitureItemPlacementDataDto trigger,
        IEnumerable<PlayerFurnitureItemPlacementDataDto> roomItems)
    {
        var candidates = roomItems is ICollection<PlayerFurnitureItemPlacementDataDto> collection
            ? tileMapHelperService.GetItemsOnTilePosition(trigger.PositionX, trigger.PositionY, collection)
            : roomItems.Where(x =>
                x.PositionX == trigger.PositionX &&
                x.PositionY == trigger.PositionY);

        var stack = candidates
            .Where(x => x.PositionZ > trigger.PositionZ)
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
            RunDetached(() => RunEffectDelayedAsync(room, effect, userWhoTriggered, strategy, delay),
                $"delayed wired effect '{GetInteractionType(effect)}'");
            return;
        }

        await strategy.ExecuteAsync(room, effect, userWhoTriggered);
        RunDetached(() => CycleInteractionStateAsync(room, effect), "wired effect state cycle");
    }

    private async Task RunEffectDelayedAsync(
        IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto effect,
        IRoomUser? userWhoTriggered,
        IWiredEffectStrategy strategy,
        int delayInPulses)
    {
        await Task.Delay(delayInPulses * _pulseMilliseconds);

        await room.RunLockedAsync(() => strategy.ExecuteAsync(room, effect, userWhoTriggered));
        await CycleInteractionStateAsync(room, effect);
    }

    private void RunDetached(Func<Task> work, string context)
    {
        using (ExecutionContext.SuppressFlow())
        {
            Task.Run(work).FireAndForget(logger, context);
        }
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
        await room.RunLockedAsync(() => furnitureItemHelperService.UpdateMetaDataForItemAsync(room, item, "1"));
        await Task.Delay(500);
        await room.RunLockedAsync(() => furnitureItemHelperService.UpdateMetaDataForItemAsync(room, item, "0"));
    }
}
