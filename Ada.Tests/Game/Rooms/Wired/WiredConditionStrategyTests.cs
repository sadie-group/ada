using System.Drawing;
using Moq;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;
using Ada.Game.Rooms.Wired.Conditions;

namespace Ada.Tests.Game.Rooms.Wired;

public class WiredConditionStrategyTests : MockHelpers
{
    private static PlayerFurnitureItemPlacementDataDto MockWiredItem(
        string interactionType,
        string intParameters = "",
        List<PlayerFurnitureItemPlacementDataDto>? selectedItems = null)
    {
        var item = MockFurnitureItemPlacementData(interactionType);

        item.WiredData = new PlayerFurnitureItemWiredDataDto
        {
            PlayerFurnitureItemPlacementDataId = item.Id,
            PlacementData = item,
            Message = "",
            IntParameters = intParameters,
            SelectedItems = selectedItems ?? []
        };

        return item;
    }

    private static IRoomLogic MockRoomWithUserCount(int count)
    {
        var room = new Mock<IRoomLogic>();
        var userRepository = new Mock<IRoomUserRepository>();

        userRepository.SetupGet(x => x.Count).Returns(count);
        room.SetupGet(x => x.UserRepository).Returns(userRepository.Object);

        return room.Object;
    }

    [Test]
    public void UserCount_WithinBounds_IsSatisfied()
    {
        var strategy = new WiredConditionUserCountStrategy();
        var condition = MockWiredItem(FurnitureItemInteractionType.WiredConditionUserCountInRoom, "2,5");

        Assert.Multiple(() =>
        {
            Assert.That(strategy.IsSatisfied(MockRoomWithUserCount(3), condition, null), Is.True);
            Assert.That(strategy.IsSatisfied(MockRoomWithUserCount(1), condition, null), Is.False);
            Assert.That(strategy.IsSatisfied(MockRoomWithUserCount(6), condition, null), Is.False);
        });
    }

    [Test]
    public void NotUserCount_WithinBounds_IsNotSatisfied()
    {
        var strategy = new WiredConditionNotUserCountStrategy();
        var condition = MockWiredItem(FurnitureItemInteractionType.WiredConditionNotUserCountInRoom, "2,5");

        Assert.Multiple(() =>
        {
            Assert.That(strategy.IsSatisfied(MockRoomWithUserCount(3), condition, null), Is.False);
            Assert.That(strategy.IsSatisfied(MockRoomWithUserCount(6), condition, null), Is.True);
        });
    }

    [Test]
    public void TriggererOnFurniture_UserOnSelectedItem_IsSatisfied()
    {
        var strategy = new WiredConditionTriggererOnFurnitureStrategy();

        var plate = MockFurnitureItemPlacementData("pressure_plate", 2, 2, id: 7);
        var condition = MockWiredItem(
            FurnitureItemInteractionType.WiredConditionTriggererOnFurniture,
            selectedItems: [plate]);

        var room = MockRoomWithUserRepoAndFurniture("x", [plate, condition]);

        var userOnPlate = new Mock<IRoomUser>();
        userOnPlate.SetupGet(x => x.Point).Returns(new Point(2, 2));

        var userElsewhere = new Mock<IRoomUser>();
        userElsewhere.SetupGet(x => x.Point).Returns(new Point(0, 0));

        Assert.Multiple(() =>
        {
            Assert.That(strategy.IsSatisfied(room, condition, userOnPlate.Object), Is.True);
            Assert.That(strategy.IsSatisfied(room, condition, userElsewhere.Object), Is.False);
            Assert.That(strategy.IsSatisfied(room, condition, null), Is.False);
        });
    }

    [Test]
    public void FurnitureHasUsers_UserOnEverySelectedItem_IsSatisfied()
    {
        var strategy = new WiredConditionFurnitureHasUsersStrategy();

        var plate = MockFurnitureItemPlacementData("pressure_plate", 2, 2, id: 7);
        var condition = MockWiredItem(
            FurnitureItemInteractionType.WiredConditionFurnitureHasUsers,
            selectedItems: [plate]);

        var userOnPlate = new Mock<IRoomUser>();
        userOnPlate.SetupGet(x => x.Point).Returns(new Point(2, 2));

        var occupied = MockRoomWithUserRepoAndFurniture("x", [plate, condition], [userOnPlate.Object]);
        var empty = MockRoomWithUserRepoAndFurniture("x", [plate, condition]);

        Assert.Multiple(() =>
        {
            Assert.That(strategy.IsSatisfied(occupied, condition, null), Is.True);
            Assert.That(strategy.IsSatisfied(empty, condition, null), Is.False);
        });
    }
}
