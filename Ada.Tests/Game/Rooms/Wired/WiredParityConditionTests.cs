using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;
using Ada.Game.Rooms.Wired;
using Ada.Game.Rooms.Wired.Conditions;
using Moq;

namespace Ada.Tests.Game.Rooms.Wired;

public class WiredParityConditionTests : MockHelpers
{
    private static PlayerFurnitureItemPlacementDataDto MockWiredItem(
        string interactionType,
        string intParameters = "",
        string message = "")
    {
        var item = MockFurnitureItemPlacementData(interactionType);

        item.WiredData = new PlayerFurnitureItemWiredDataDto
        {
            PlayerFurnitureItemPlacementDataId = item.Id,
            PlacementData = item,
            Message = message,
            IntParameters = intParameters
        };

        return item;
    }

    private static IRoomLogic MockRoom(int roomId = 1)
    {
        var roomDto = new Ada.API.DTOs.Rooms.RoomDto { Id = roomId, FurnitureItems = [] };
        var room = new Mock<IRoomLogic>();
        room.SetupGet(x => x.Room).Returns(roomDto);
        return room.Object;
    }

    [Test]
    public void TimeElapsed_FreshTimer_LessPassesMoreFails()
    {
        var timerService = new WiredTimerService();
        timerService.Reset(1);

        var more = new WiredConditionTimeElapsedMoreStrategy(timerService);
        var less = new WiredConditionTimeElapsedLessStrategy(timerService);

        var moreCondition = MockWiredItem(FurnitureItemInteractionType.WiredConditionTimeElapsedMore, "20");
        var lessCondition = MockWiredItem(FurnitureItemInteractionType.WiredConditionTimeElapsedLess, "20");

        Assert.Multiple(() =>
        {
            Assert.That(more.IsSatisfied(MockRoom(), moreCondition, null), Is.False);
            Assert.That(less.IsSatisfied(MockRoom(), lessCondition, null), Is.True);
        });
    }

    [Test]
    public void DateRange_NowInsideAndOutside_EvaluatesCorrectly()
    {
        var strategy = new WiredConditionDateRangeActiveStrategy();
        var now = DateTimeOffset.Now.ToUnixTimeSeconds();

        var active = MockWiredItem(
            FurnitureItemInteractionType.WiredConditionDateRangeActive, $"{now - 100},{now + 100}");
        var expired = MockWiredItem(
            FurnitureItemInteractionType.WiredConditionDateRangeActive, $"{now - 200},{now - 100}");

        Assert.Multiple(() =>
        {
            Assert.That(strategy.IsSatisfied(MockRoom(), active, null), Is.True);
            Assert.That(strategy.IsSatisfied(MockRoom(), expired, null), Is.False);
        });
    }

    [Test]
    public void WearsEffect_MatchesActiveEffectId()
    {
        var strategy = new WiredConditionTriggererWearsEffectStrategy();
        var condition = MockWiredItem(FurnitureItemInteractionType.WiredConditionTriggererWearsEffect, "5");

        var wearing = new Mock<IRoomUser>();
        wearing.SetupGet(x => x.ActiveEffectId).Returns(5);

        var notWearing = new Mock<IRoomUser>();
        notWearing.SetupGet(x => x.ActiveEffectId).Returns(3);

        Assert.Multiple(() =>
        {
            Assert.That(strategy.IsSatisfied(MockRoom(), condition, wearing.Object), Is.True);
            Assert.That(strategy.IsSatisfied(MockRoom(), condition, notWearing.Object), Is.False);
            Assert.That(strategy.IsSatisfied(MockRoom(), condition, null), Is.False);
        });
    }

    [Test]
    public void HasHandItem_MatchesHandItemId()
    {
        var strategy = new WiredConditionTriggererHasHandItemStrategy();
        var specific = MockWiredItem(FurnitureItemInteractionType.WiredConditionTriggererHasHandItem, "2");
        var any = MockWiredItem(FurnitureItemInteractionType.WiredConditionTriggererHasHandItem);

        var holding = new Mock<IRoomUser>();
        holding.SetupGet(x => x.HandItemId).Returns(2);

        var empty = new Mock<IRoomUser>();
        empty.SetupGet(x => x.HandItemId).Returns(0);

        Assert.Multiple(() =>
        {
            Assert.That(strategy.IsSatisfied(MockRoom(), specific, holding.Object), Is.True);
            Assert.That(strategy.IsSatisfied(MockRoom(), any, holding.Object), Is.True);
            Assert.That(strategy.IsSatisfied(MockRoom(), specific, empty.Object), Is.False);
        });
    }

    [Test]
    public void TimerService_ResetRearmsFiredTriggers()
    {
        var timerService = new WiredTimerService();

        Assert.Multiple(() =>
        {
            Assert.That(timerService.TryMarkFired(1, 10), Is.True);
            Assert.That(timerService.TryMarkFired(1, 10), Is.False);
        });

        timerService.Reset(1);

        Assert.That(timerService.TryMarkFired(1, 10), Is.True);
    }
}
