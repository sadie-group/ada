using System.Drawing;
using Ada.API;
using Ada.API.DTOs.Players;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Pathfinding;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.Core.Enums.Game.Rooms;
using Ada.Core.Enums.Miscellaneous;
using Ada.Db.Models.Constants;
using Ada.Game.Rooms.Mapping;
using Ada.Game.Rooms.Users;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Ada.Tests.Game.Rooms.Users;

[TestFixture]
public class RoomUserFactoryTests
{
    [Test]
    public void Create_ResolvesRoomUserWithSuppliedArguments()
    {
        var provider = new ServiceCollection()
            .AddSingleton(new ServerRoomConstants { SecondsTillUserIdle = 3600 })
            .AddSingleton(Mock.Of<IRoomTileMapHelperService>())
            .AddSingleton(Mock.Of<IRoomHelperService>())
            .AddSingleton(Mock.Of<IRoomWiredService>())
            .AddSingleton(Mock.Of<IRoomPathFinderHelperService>())
            .AddSingleton(Mock.Of<IRoomFurnitureItemInteractorRepository>())
            .BuildServiceProvider();

        var roomDto = new RoomDto { FurnitureItems = [], Settings = new RoomSettingsDto() };
        var room = new Mock<IRoomLogic>();
        room.SetupGet(x => x.Room).Returns(roomDto);
        room.SetupGet(x => x.TileMap).Returns(new RoomTileMap("0", []));

        var playerData = new PlayerDto(
            1L,
            "TestUser",
            "test@example.com",
            DateTimeOffset.UtcNow,
            [],
            new PlayerDataDto(),
            new PlayerAvatarDataDto(),
            [],
            [],
            [],
            [],
            new PlayerNavigatorSettingsDto(),
            new PlayerGameSettingsDto(),
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            []);
        var player = new Mock<IPlayerLogic>();
        player.SetupGet(x => x.Player).Returns(playerData);

        var networkObject = new Mock<INetworkObject>().Object;
        var factory = new RoomUserFactory(provider);

        var user = factory.Create(
            room.Object,
            networkObject,
            new Point(2, 3),
            1.5,
            HDirection.East,
            HDirection.South,
            player.Object,
            RoomControllerLevel.Owner);

        Assert.Multiple(() =>
        {
            Assert.That(user, Is.InstanceOf<RoomUser>());
            Assert.That(user.Point, Is.EqualTo(new Point(2, 3)));
            Assert.That(user.Player, Is.SameAs(player.Object));
            Assert.That(user.NetworkObject, Is.SameAs(networkObject));
        });
    }
}
