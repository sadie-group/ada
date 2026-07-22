using AutoMapper;
using Moq;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Services.Wired;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;
using Ada.Game.Rooms.Furniture;
using Ada.Game.Rooms.Services;
using Ada.Game.Rooms.Wired;
using Ada.Tests.Common;

namespace Ada.Tests.Game.Rooms.Wired;

public class RoomWiredServiceConditionTests : MockHelpers
{
    private class RecordingEffectStrategy : IWiredEffectStrategy
    {
        public int Executions;

        public string InteractionType => FurnitureItemInteractionType.WiredEffectShowMessage;

        public Task ExecuteAsync(
            IRoomLogic room,
            PlayerFurnitureItemPlacementDataDto effect,
            IRoomUser? userWhoTriggered)
        {
            Executions++;
            return Task.CompletedTask;
        }
    }

    private class FixedConditionStrategy(bool satisfied) : IWiredConditionStrategy
    {
        public string InteractionType => FurnitureItemInteractionType.WiredConditionUserCountInRoom;

        public bool IsSatisfied(
            IRoomLogic room,
            PlayerFurnitureItemPlacementDataDto condition,
            IRoomUser? userWhoTriggered) => satisfied;
    }

    private static PlayerFurnitureItemPlacementDataDto WithWiredData(PlayerFurnitureItemPlacementDataDto item)
    {
        item.WiredData = new PlayerFurnitureItemWiredDataDto
        {
            PlayerFurnitureItemPlacementDataId = item.Id,
            PlacementData = item,
            Message = ""
        };

        return item;
    }

    private static async Task<int> RunStackAsync(bool conditionSatisfied)
    {
        var dbFactory = TestDbFactory.CreateDbFactory();
        var playerRepository = CreatePlayerRepositoryMock();
        var mapper = new Mock<IMapper>();
        var helperService = new RoomFurnitureItemHelperService(dbFactory, playerRepository.Object, mapper.Object);

        var effectStrategy = new RecordingEffectStrategy();

        var wiredService = new RoomWiredService(
            dbFactory,
            helperService,
            [effectStrategy],
            [new FixedConditionStrategy(conditionSatisfied)],
            new WiredTimerService());

        var trigger = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredTriggerEnterRoom, id: 1));
        var condition = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredConditionUserCountInRoom, 0, 0, 1, id: 2));
        var effect = WithWiredData(MockFurnitureItemPlacementData(FurnitureItemInteractionType.WiredEffectShowMessage, 0, 0, 2, id: 3));

        var room = MockRoomWithUserRepoAndFurniture("x", [trigger, condition, effect]);

        await wiredService.RunTriggerForRoomAsync(room, trigger, null);

        return effectStrategy.Executions;
    }

    [Test]
    public async Task RunTrigger_ConditionFails_EffectDoesNotRun()
    {
        Assert.That(await RunStackAsync(conditionSatisfied: false), Is.EqualTo(0));
    }

    [Test]
    public async Task RunTrigger_ConditionPasses_EffectRuns()
    {
        Assert.That(await RunStackAsync(conditionSatisfied: true), Is.EqualTo(1));
    }
}
