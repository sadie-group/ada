using System.Drawing;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Enums.Miscellaneous;
using Ada.Game.Rooms.Mapping;
using Ada.Game.Rooms.Wired.Effects;
using Moq;

namespace Ada.Tests.Game.Rooms.Wired;

[TestFixture]
public class WiredEffectChaseStrategyTests : MockHelpers
{
    private static PlayerFurnitureItemPlacementDataDto MockWiredItem(
        string interactionType,
        List<PlayerFurnitureItemPlacementDataDto>? selectedItems = null)
    {
        var item = MockFurnitureItemPlacementData(interactionType);

        item.WiredData = new PlayerFurnitureItemWiredDataDto
        {
            PlayerFurnitureItemPlacementDataId = item.Id,
            PlacementData = item,
            Message = "",
            IntParameters = "",
            SelectedItems = selectedItems ?? []
        };

        return item;
    }

    private static Mock<IRoomLogic> MockRoom(
        List<PlayerFurnitureItemPlacementDataDto> furnitureItems,
        List<IRoomUser>? users = null)
    {
        var roomDto = new RoomDto { FurnitureItems = furnitureItems };

        var room = new Mock<IRoomLogic>();
        room.SetupGet(x => x.Room).Returns(roomDto);

        var userRepository = new Mock<IRoomUserRepository>();
        userRepository.Setup(x => x.GetAll()).Returns(users ?? []);
        room.SetupGet(x => x.UserRepository).Returns(userRepository.Object);
        room.SetupGet(x => x.TileMap).Returns(new RoomTileMap("x", furnitureItems));

        return room;
    }

    private static IRoomUser MockUserAt(Point point)
    {
        var user = new Mock<IRoomUser>();
        user.SetupGet(x => x.Point).Returns(point);
        return user.Object;
    }

    [Test]
    public void InteractionType_IsChaseEffect()
    {
        var strategy = new WiredEffectChaseStrategy(
            Mock.Of<IRoomTileMapHelperService>(),
            Mock.Of<IRoomFurnitureItemHelperService>());

        Assert.That(strategy.InteractionType,
            Is.EqualTo(FurnitureItemInteractionType.WiredEffectMoveFurnitureToClosestUser));
    }

    [Test]
    public async Task Execute_NoUsersInRoom_DoesNothing()
    {
        var tileHelper = new Mock<IRoomTileMapHelperService>();
        var furniHelper = new Mock<IRoomFurnitureItemHelperService>();
        var strategy = new WiredEffectChaseStrategy(tileHelper.Object, furniHelper.Object);

        var item = MockFurnitureItemPlacementData("lamp", 1, 1, id: 4);
        var room = MockRoom([item]);

        await strategy.ExecuteAsync(room.Object, MockWiredItem("x", [item]), null);

        tileHelper.Verify(
            x => x.CanPlaceAt(It.IsAny<IEnumerable<Point>>(), It.IsAny<IRoomTileMap>(),
                It.IsAny<ICollection<PlayerFurnitureItemPlacementDataDto>>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Test]
    public async Task Execute_MovesItemTowardsClosestUser()
    {
        var tileHelper = new Mock<IRoomTileMapHelperService>();
        var furniHelper = new Mock<IRoomFurnitureItemHelperService>();
        var strategy = new WiredEffectChaseStrategy(tileHelper.Object, furniHelper.Object);

        var item = MockFurnitureItemPlacementData("lamp", 0, 0, id: 4);
        var closeUser = MockUserAt(new Point(3, 1));
        var farUser = MockUserAt(new Point(10, 10));
        var room = MockRoom([item], [closeUser, farUser]);

        tileHelper
            .Setup(x => x.GetPointInFront(0, 0, HDirection.East, 0))
            .Returns(new Point(1, 0));
        tileHelper
            .Setup(x => x.CanPlaceAt(It.IsAny<IEnumerable<Point>>(), It.IsAny<IRoomTileMap>(),
                It.IsAny<ICollection<PlayerFurnitureItemPlacementDataDto>>(), It.IsAny<bool>()))
            .Returns(true);

        await strategy.ExecuteAsync(room.Object, MockWiredItem("x", [item]), null);

        Assert.Multiple(() =>
        {
            Assert.That(item.PositionX, Is.EqualTo(1));
            Assert.That(item.PositionY, Is.EqualTo(0));
        });

        tileHelper.Verify(x => x.GetPointInFront(0, 0, HDirection.East, 0), Times.Once);
        furniHelper.Verify(x => x.BroadcastItemUpdateToRoomAsync(room.Object, item), Times.Once);
    }
}
