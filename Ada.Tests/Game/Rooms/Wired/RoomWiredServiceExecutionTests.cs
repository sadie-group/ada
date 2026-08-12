using Ada.Game.Rooms.Mapping;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Services.Wired;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Enums.Game.Rooms.Furniture;
using Ada.Core.Enums.Miscellaneous;
using Ada.Db;
using Ada.Db.Models.Furniture;
using Ada.Db.Models.Players;
using Ada.Db.Models.Players.Furniture;
using Ada.Db.Models.Rooms;
using Ada.Game.Rooms.Services;
using Ada.Game.Rooms.Wired;
using Ada.Tests.Common;
using Moq;

namespace Ada.Tests.Game.Rooms.Wired;

public class RoomWiredServiceExecutionTests : MockHelpers
{
    private sealed class RecordingEffectStrategy(string interactionType) : IWiredEffectStrategy
    {
        private readonly TaskCompletionSource _executed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Executions;

        public string InteractionType => interactionType;

        public Task WaitForExecutionAsync() => _executed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        public Task ExecuteAsync(
            IRoomLogic room,
            PlayerFurnitureItemPlacementDataDto effect,
            IRoomUser? userWhoTriggered)
        {
            Executions++;
            _executed.TrySetResult();
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingHelperService : IRoomFurnitureItemHelperService
    {
        private readonly TaskCompletionSource _cycleCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public readonly List<string> MetaDataUpdates = [];

        public Task WaitForCycleAsync() => _cycleCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        public Task CycleInteractionStateForItemAsync(
            IRoomLogic room,
            PlayerFurnitureItemPlacementDataDto roomFurnitureItem) => Task.CompletedTask;

        public Task UpdateMetaDataForItemAsync(
            IRoomLogic room,
            PlayerFurnitureItemPlacementDataDto roomFurnitureItem,
            string metaData)
        {
            MetaDataUpdates.Add(metaData);

            if (metaData == "0")
            {
                _cycleCompleted.TrySetResult();
            }

            return Task.CompletedTask;
        }

        public Task BroadcastItemUpdateToRoomAsync(
            IRoomLogic room,
            PlayerFurnitureItemPlacementDataDto roomFurnitureItem) => Task.CompletedTask;

        public ObjectDataKey GetObjectDataKeyForItem(PlayerFurnitureItemPlacementDataDto furnitureItem) => default;

        public Dictionary<string, string> GetObjectDataForItem(PlayerFurnitureItemPlacementDataDto furnitureItem) => [];
    }

    private static PlayerFurnitureItemPlacementDataDto WithWiredData(
        PlayerFurnitureItemPlacementDataDto item,
        int delay = 0,
        string message = "",
        List<PlayerFurnitureItemPlacementDataDto>? selectedItems = null)
    {
        item.WiredData = new PlayerFurnitureItemWiredDataDto
        {
            PlayerFurnitureItemPlacementDataId = item.Id,
            PlacementData = item,
            Message = message,
            Delay = delay,
            SelectedItems = selectedItems ?? []
        };

        return item;
    }

    private static RoomWiredService CreateService(
        IDbContextFactory<AdaDbContext>? dbFactory = null,
        RecordingHelperService? helperService = null,
        IWiredTimerService? timerService = null,
        IWiredEffectStrategy[]? effectStrategies = null)
    {
        return new RoomWiredService(
            dbFactory ?? TestDbFactory.CreateDbFactory(),
            helperService ?? new RecordingHelperService(),
            new RoomTileMapHelperService(),
            effectStrategies ?? [],
            [],
            timerService ?? new WiredTimerService(),
            NullLogger<RoomWiredService>.Instance);
    }

    [Test]
    public void GetTriggers_FiltersByInteractionTypeAndWiredData()
    {
        var service = CreateService();
        var matching = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredTriggerSaysSomething, id: 1));
        var wrongType = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredTriggerEnterRoom, id: 2));
        var noWiredData = MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredTriggerSaysSomething, id: 3);

        var triggers = service.GetTriggers(
            FurnitureItemInteractionType.WiredTriggerSaysSomething,
            [matching, wrongType, noWiredData]).ToList();

        Assert.That(triggers.Select(x => x.Id), Is.EqualTo(new[] { 1 }));
    }

    [TestCase("well HELLO there", true)]
    [TestCase("goodbye", false)]
    [TestCase("", true)]
    public void GetTriggers_MatchesWiredMessageAgainstRequiredMessage(string requiredMessage, bool expected)
    {
        var service = CreateService();
        var trigger = WithWiredData(
            MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredTriggerSaysSomething, id: 1),
            message: "hello");

        var triggers = service.GetTriggers(
            FurnitureItemInteractionType.WiredTriggerSaysSomething,
            [trigger],
            requiredMessage);

        Assert.That(triggers.Any(), Is.EqualTo(expected));
    }

    [Test]
    public void GetTriggers_EmptyWiredMessage_MatchesAnyRequiredMessage()
    {
        var service = CreateService();
        var trigger = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredTriggerSaysSomething, id: 1));

        var triggers = service.GetTriggers(
            FurnitureItemInteractionType.WiredTriggerSaysSomething,
            [trigger],
            "anything");

        Assert.That(triggers.Any(), Is.True);
    }

    [TestCase(5, true)]
    [TestCase(6, false)]
    public void GetTriggers_FiltersBySelectedIds(int requiredId, bool expected)
    {
        var service = CreateService();
        var selected = MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredEffectShowMessage, id: 5);
        var trigger = WithWiredData(
            MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredTriggerSaysSomething, id: 1),
            selectedItems: [selected]);

        var triggers = service.GetTriggers(
            FurnitureItemInteractionType.WiredTriggerSaysSomething,
            [trigger],
            requiredSelectedIds: [requiredId]);

        Assert.That(triggers.Any(), Is.EqualTo(expected));
    }

    [Test]
    public void GetEffectsForTrigger_ReturnsOrderedEffectsAboveTrigger()
    {
        var service = CreateService();
        var trigger = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredTriggerEnterRoom, id: 1));
        var effectLow = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredEffectShowMessage, 0, 0, 1, id: 2));
        var condition = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredConditionUserCountInRoom, 0, 0, 2, id: 3));
        var effectHigh = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredEffectKickUser, 0, 0, 3, id: 4));
        var elsewhere = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredEffectShowMessage, 1, 1, 1, id: 5));

        var effects = service.GetEffectsForTrigger(trigger, [trigger, effectHigh, condition, effectLow, elsewhere]).ToList();

        Assert.That(effects.Select(x => x.Id), Is.EqualTo(new[] { 2, 4 }));
    }

    [Test]
    public void GetEffectsForTrigger_StopsAtNonWiredItem()
    {
        var service = CreateService();
        var trigger = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredTriggerEnterRoom, id: 1));
        var effect = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredEffectShowMessage, 0, 0, 1, id: 2));
        var untyped = MockFurnitureItemPlacementData("", 0, 0, 2, id: 3);
        var blocker = MockFurnitureItemPlacementData("default", 0, 0, 3, id: 4);
        var aboveBlocker = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredEffectShowMessage, 0, 0, 4, id: 5));

        var effects = service.GetEffectsForTrigger(trigger, [trigger, effect, untyped, blocker, aboveBlocker]).ToList();

        Assert.That(effects.Select(x => x.Id), Is.EqualTo(new[] { 2 }));
    }

    [Test]
    public async Task RunTrigger_EffectWithoutWiredData_IsNotExecuted()
    {
        var strategy = new RecordingEffectStrategy(FurnitureItemInteractionType.WiredEffectShowMessage);
        var service = CreateService(effectStrategies: [strategy]);
        var trigger = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredTriggerEnterRoom, id: 1));
        var effect = MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredEffectShowMessage, 0, 0, 1, id: 2);
        var room = MockRoomWithUserRepoAndFurniture("x", [trigger, effect]);

        await service.RunTriggerForRoomAsync(room, trigger, null);

        Assert.That(strategy.Executions, Is.Zero);
    }

    [Test]
    public async Task RunTrigger_EffectWithoutStrategy_IsNotExecuted()
    {
        var strategy = new RecordingEffectStrategy(FurnitureItemInteractionType.WiredEffectShowMessage);
        var service = CreateService(effectStrategies: [strategy]);
        var trigger = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredTriggerEnterRoom, id: 1));
        var effect = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredEffectKickUser, 0, 0, 1, id: 2));
        var room = MockRoomWithUserRepoAndFurniture("x", [trigger, effect]);

        await service.RunTriggerForRoomAsync(room, trigger, null);

        Assert.That(strategy.Executions, Is.Zero);
    }

    [Test]
    public async Task RunTrigger_DelayedEffect_ExecutesAfterDelay()
    {
        var strategy = new RecordingEffectStrategy(FurnitureItemInteractionType.WiredEffectShowMessage);
        var service = CreateService(effectStrategies: [strategy]);
        var trigger = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredTriggerEnterRoom, id: 1));
        var effect = WithWiredData(
            MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredEffectShowMessage, 0, 0, 1, id: 2),
            delay: 1);
        var room = MockRoomWithUserRepoAndFurniture("x", [trigger, effect]);

        await service.RunTriggerForRoomAsync(room, trigger, null);

        Assert.That(strategy.Executions, Is.Zero);

        await strategy.WaitForExecutionAsync();

        Assert.That(strategy.Executions, Is.EqualTo(1));
    }

    [Test]
    public async Task RunTrigger_CyclesTriggerInteractionState()
    {
        var helperService = new RecordingHelperService();
        var service = CreateService(helperService: helperService);
        var trigger = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredTriggerEnterRoom, id: 1));
        var room = MockRoomWithUserRepoAndFurniture("x", [trigger]);

        await service.RunTriggerForRoomAsync(room, trigger, null);
        await helperService.WaitForCycleAsync();

        Assert.That(helperService.MetaDataUpdates, Is.EqualTo(new[] { "1", "0" }));
    }

    [Test]
    public async Task RunPeriodicTriggers_NoPeriodicTriggers_DoesNothing()
    {
        var strategy = new RecordingEffectStrategy(FurnitureItemInteractionType.WiredEffectShowMessage);
        var service = CreateService(effectStrategies: [strategy]);
        var trigger = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredTriggerEnterRoom, id: 1));
        var room = MockRoomWithUserRepoAndFurniture("x", [trigger]);

        await service.RunPeriodicTriggersForRoomAsync(room);

        Assert.That(strategy.Executions, Is.Zero);
    }

    [Test]
    public async Task RunPeriodicTriggers_TriggerWithoutWiredData_IsSkipped()
    {
        var strategy = new RecordingEffectStrategy(FurnitureItemInteractionType.WiredEffectShowMessage);
        var service = CreateService(effectStrategies: [strategy]);
        var trigger = MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredTriggerPeriodically, id: 9101);
        var room = MockRoomWithUserRepoAndFurniture("x", [trigger]);

        await service.RunPeriodicTriggersForRoomAsync(room);

        Assert.That(strategy.Executions, Is.Zero);
    }

    [Test]
    public async Task RunPeriodicTriggers_AtGivenTimeDue_Runs()
    {
        var strategy = new RecordingEffectStrategy(FurnitureItemInteractionType.WiredEffectShowMessage);
        var timerService = new Mock<IWiredTimerService>();
        timerService.Setup(x => x.GetElapsed(It.IsAny<long>())).Returns(TimeSpan.FromHours(1));
        timerService.Setup(x => x.TryMarkFired(It.IsAny<long>(), It.IsAny<int>())).Returns(true);
        var service = CreateService(timerService: timerService.Object, effectStrategies: [strategy]);
        var trigger = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredTriggerAtGivenTime, id: 9201));
        var effect = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredEffectShowMessage, 0, 0, 1, id: 9202));
        var room = MockRoomWithUserRepoAndFurniture("x", [trigger, effect]);

        await service.RunPeriodicTriggersForRoomAsync(room);

        Assert.That(strategy.Executions, Is.EqualTo(1));
    }

    [Test]
    public async Task RunPeriodicTriggers_AtGivenTimeAlreadyFired_DoesNotRun()
    {
        var strategy = new RecordingEffectStrategy(FurnitureItemInteractionType.WiredEffectShowMessage);
        var timerService = new Mock<IWiredTimerService>();
        timerService.Setup(x => x.GetElapsed(It.IsAny<long>())).Returns(TimeSpan.FromHours(1));
        timerService.Setup(x => x.TryMarkFired(It.IsAny<long>(), It.IsAny<int>())).Returns(false);
        var service = CreateService(timerService: timerService.Object, effectStrategies: [strategy]);
        var trigger = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredTriggerAtGivenTime, id: 9301));
        var effect = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredEffectShowMessage, 0, 0, 1, id: 9302));
        var room = MockRoomWithUserRepoAndFurniture("x", [trigger, effect]);

        await service.RunPeriodicTriggersForRoomAsync(room);

        Assert.That(strategy.Executions, Is.Zero);
    }

    [Test]
    public async Task RunPeriodicTriggers_AtGivenTimeNotDue_DoesNotRun()
    {
        var strategy = new RecordingEffectStrategy(FurnitureItemInteractionType.WiredEffectShowMessage);
        var timerService = new Mock<IWiredTimerService>();
        timerService.Setup(x => x.GetElapsed(It.IsAny<long>())).Returns(TimeSpan.Zero);
        var service = CreateService(timerService: timerService.Object, effectStrategies: [strategy]);
        var trigger = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredTriggerAtGivenTime, id: 9401));
        var effect = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredEffectShowMessage, 0, 0, 1, id: 9402));
        var room = MockRoomWithUserRepoAndFurniture("x", [trigger, effect]);

        await service.RunPeriodicTriggersForRoomAsync(room);

        Assert.That(strategy.Executions, Is.Zero);
        timerService.Verify(x => x.TryMarkFired(It.IsAny<long>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task RunPeriodicTriggers_Periodically_RunsAfterIntervalElapsed()
    {
        var strategy = new RecordingEffectStrategy(FurnitureItemInteractionType.WiredEffectShowMessage);
        var service = CreateService(effectStrategies: [strategy]);
        var trigger = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredTriggerPeriodically, id: 9501));
        var effect = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredEffectShowMessage, 0, 0, 1, id: 9502));
        var room = MockRoomWithUserRepoAndFurniture("x", [trigger, effect]);

        await service.RunPeriodicTriggersForRoomAsync(room);

        Assert.That(strategy.Executions, Is.Zero);

        await Task.Delay(650);
        await service.RunPeriodicTriggersForRoomAsync(room);

        Assert.That(strategy.Executions, Is.EqualTo(1));
    }

    [Test]
    public async Task RunPeriodicTriggers_PeriodicallyLong_FirstPassOnlyRecordsLastRun()
    {
        var strategy = new RecordingEffectStrategy(FurnitureItemInteractionType.WiredEffectShowMessage);
        var service = CreateService(effectStrategies: [strategy]);
        var trigger = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredTriggerPeriodicallyLong, id: 9601));
        var effect = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredEffectShowMessage, 0, 0, 1, id: 9602));
        var room = MockRoomWithUserRepoAndFurniture("x", [trigger, effect]);

        await service.RunPeriodicTriggersForRoomAsync(room);
        await service.RunPeriodicTriggersForRoomAsync(room);

        Assert.That(strategy.Executions, Is.Zero);
    }

    private static readonly (string InteractionType, int ExpectedCode)[] WiredCodes =
    [
        (FurnitureItemInteractionType.WiredTriggerSaysSomething, (int) WiredTriggerCode.AvatarSaysSomething),
        (FurnitureItemInteractionType.WiredTriggerEnterRoom, (int) WiredTriggerCode.AvatarEntersRoom),
        (FurnitureItemInteractionType.WiredTriggerUserWalksOnFurniture, (int) WiredTriggerCode.AvatarWalksOnFurniture),
        (FurnitureItemInteractionType.WiredTriggerUserWalksOffFurniture, (int) WiredTriggerCode.AvatarWalksOffFurniture),
        (FurnitureItemInteractionType.WiredTriggerFurnitureStateChanged, (int) WiredTriggerCode.ToggleFurniture),
        (FurnitureItemInteractionType.WiredTriggerPeriodically, (int) WiredTriggerCode.ExecutePeriodically),
        (FurnitureItemInteractionType.WiredTriggerPeriodicallyLong, (int) WiredTriggerCode.ExecutePeriodicallyLong),
        (FurnitureItemInteractionType.WiredTriggerAtGivenTime, (int) WiredTriggerCode.ExecuteOnce),
        (FurnitureItemInteractionType.WiredEffectShowMessage, (int) WiredEffectCode.ShowMessage),
        (FurnitureItemInteractionType.WiredEffectKickUser, (int) WiredEffectCode.KickUser),
        (FurnitureItemInteractionType.WiredEffectToggleFurnitureState, (int) WiredEffectCode.ToggleFurnitureState),
        (FurnitureItemInteractionType.WiredEffectTeleportToFurniture, (int) WiredEffectCode.TeleportToFurniture),
        (FurnitureItemInteractionType.WiredEffectResetTimers, (int) WiredEffectCode.TimerReset),
        (FurnitureItemInteractionType.WiredEffectMoveRotateFurniture, (int) WiredEffectCode.MoveRotateFurniture),
        (FurnitureItemInteractionType.WiredEffectMoveFurnitureToClosestUser, (int) WiredEffectCode.MoveFurnitureToClosestUser),
        (FurnitureItemInteractionType.WiredEffectFleeFromClosestUser, (int) WiredEffectCode.FleeFromClosestUser),
        (FurnitureItemInteractionType.WiredEffectChangeFurnitureDirection, (int) WiredEffectCode.ChangeFurnitureDirection),
        (FurnitureItemInteractionType.WiredEffectCallAnotherStack, (int) WiredEffectCode.CallAnotherStack),
        (FurnitureItemInteractionType.WiredEffectMuteTriggerer, (int) WiredEffectCode.MuteTriggerer),
        (FurnitureItemInteractionType.WiredConditionFurnitureHasUsers, (int) WiredConditionCode.FurnitureHasUsers),
        (FurnitureItemInteractionType.WiredConditionNotFurnitureHasUsers, (int) WiredConditionCode.NotFurnitureHasUsers),
        (FurnitureItemInteractionType.WiredConditionTriggererOnFurniture, (int) WiredConditionCode.TriggererOnFurniture),
        (FurnitureItemInteractionType.WiredConditionNotTriggererOnFurniture, (int) WiredConditionCode.NotTriggererOnFurniture),
        (FurnitureItemInteractionType.WiredConditionUserCountInRoom, (int) WiredConditionCode.UserCountInRoom),
        (FurnitureItemInteractionType.WiredConditionNotUserCountInRoom, (int) WiredConditionCode.NotUserCountInRoom),
        (FurnitureItemInteractionType.WiredConditionTimeElapsedMore, (int) WiredConditionCode.TimeElapsedMore),
        (FurnitureItemInteractionType.WiredConditionTimeElapsedLess, (int) WiredConditionCode.TimeElapsedLess),
        (FurnitureItemInteractionType.WiredConditionFurnitureHasFurniture, (int) WiredConditionCode.FurnitureHasFurniture),
        (FurnitureItemInteractionType.WiredConditionNotFurnitureHasFurniture, (int) WiredConditionCode.NotFurnitureHasFurniture),
        (FurnitureItemInteractionType.WiredConditionTriggererWearsBadge, (int) WiredConditionCode.TriggererWearsBadge),
        (FurnitureItemInteractionType.WiredConditionNotTriggererWearsBadge, (int) WiredConditionCode.NotTriggererWearsBadge),
        (FurnitureItemInteractionType.WiredConditionTriggererWearsEffect, (int) WiredConditionCode.TriggererWearsEffect),
        (FurnitureItemInteractionType.WiredConditionNotTriggererWearsEffect, (int) WiredConditionCode.NotTriggererWearsEffect),
        (FurnitureItemInteractionType.WiredConditionTriggererHasHandItem, (int) WiredConditionCode.TriggererHasHandItem),
        (FurnitureItemInteractionType.WiredConditionDateRangeActive, (int) WiredConditionCode.DateRangeActive)
    ];

    [Test]
    public void GetWiredCode_MapsEveryInteractionType()
    {
        var service = CreateService();

        Assert.Multiple(() =>
        {
            foreach (var (interactionType, expectedCode) in WiredCodes)
            {
                Assert.That(service.GetWiredCode(interactionType), Is.EqualTo(expectedCode));
            }
        });
    }

    [Test]
    public void GetWiredCode_UnknownInteractionType_Throws()
    {
        var service = CreateService();

        Assert.Throws<ArgumentException>(() => service.GetWiredCode("default"));
    }

    private static async Task<SqliteTestDbFactory> CreateSeededSqliteFactoryAsync()
    {
        var factory = new SqliteTestDbFactory();

        await using var db = factory.CreateDbContext();

        var player = new Player { Id = 1, Username = "u", Email = "e", Password = "p" };
        db.Players.Add(player);
        db.RoomLayouts.Add(new RoomLayout { Id = 1 });
        db.Rooms.Add(new Room { Id = 1, Name = "r", Description = "", OwnerId = 1, LayoutId = 1 });

        var furnitureItem = new FurnitureItem
        {
            Id = 1,
            Name = "wired",
            AssetName = "wired",
            InteractionType = FurnitureItemInteractionType.WiredEffectShowMessage
        };
        db.FurnitureItems.Add(furnitureItem);

        var firstItem = new PlayerFurnitureItem
            { Id = 10, Player = player, FurnitureItemId = 1, FurnitureItem = furnitureItem, LimitedData = "", MetaData = "" };
        var secondItem = new PlayerFurnitureItem
            { Id = 11, Player = player, FurnitureItemId = 1, FurnitureItem = furnitureItem, LimitedData = "", MetaData = "" };
        db.PlayerFurnitureItems.AddRange(firstItem, secondItem);

        var firstPlacement = new PlayerFurnitureItemPlacementData { Id = 10, PlayerFurnitureItem = firstItem, RoomId = 1 };
        var secondPlacement = new PlayerFurnitureItemPlacementData { Id = 11, PlayerFurnitureItem = secondItem, RoomId = 1 };
        db.RoomFurnitureItems.AddRange(firstPlacement, secondPlacement);

        db.Add(new PlayerFurnitureItemWiredData
        {
            PlayerFurnitureItemPlacementDataId = 10,
            PlacementData = firstPlacement,
            Message = "old"
        });

        await db.SaveChangesAsync();

        return factory;
    }

    [Test]
    public async Task SaveSettings_ReplacesExistingRowAndLinksSelectedItems()
    {
        using var factory = await CreateSeededSqliteFactoryAsync();
        var service = CreateService(dbFactory: factory);

        var placementDto = MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredEffectShowMessage, id: 10);
        var selectedDto = MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredEffectShowMessage, id: 11);
        var wiredDto = new PlayerFurnitureItemWiredDataDto
        {
            PlayerFurnitureItemPlacementDataId = 10,
            PlacementData = placementDto,
            Message = "new",
            IntParameters = "5,7",
            Delay = 2,
            SelectedItems = [selectedDto]
        };

        await service.SaveSettingsAsync(placementDto, wiredDto);

        await using (var db = factory.CreateDbContext())
        {
            var rows = db.Set<PlayerFurnitureItemWiredData>()
                .Include(x => x.SelectedItems)
                .Where(x => x.PlayerFurnitureItemPlacementDataId == 10)
                .ToList();

            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(rows[0].Message, Is.EqualTo("new"));
                Assert.That(rows[0].IntParameters, Is.EqualTo("5,7"));
                Assert.That(rows[0].Delay, Is.EqualTo(2));
                Assert.That(rows[0].SelectedItems.Select(x => x.Id), Is.EqualTo(new[] { 11 }));
            });
        }

        Assert.That(placementDto.WiredData, Is.SameAs(wiredDto));
    }

    [Test]
    public async Task SaveSettings_NoSelectedItems_SavesEmptySelection()
    {
        using var factory = await CreateSeededSqliteFactoryAsync();
        var service = CreateService(dbFactory: factory);

        var placementDto = MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredEffectShowMessage, id: 10);
        var wiredDto = new PlayerFurnitureItemWiredDataDto
        {
            PlayerFurnitureItemPlacementDataId = 10,
            PlacementData = placementDto,
            Message = ""
        };

        await service.SaveSettingsAsync(placementDto, wiredDto);

        await using var db = factory.CreateDbContext();

        var row = db.Set<PlayerFurnitureItemWiredData>()
            .Include(x => x.SelectedItems)
            .Single(x => x.PlayerFurnitureItemPlacementDataId == 10);
        Assert.Multiple(() =>
        {
            Assert.That(row.Message, Is.Empty);
            Assert.That(row.SelectedItems, Is.Empty);
        });
    }
}
