using System.Drawing;
using Ada.API;
using Ada.API.DTOs.Furniture;
using Ada.API.DTOs.Players;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Networking;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Enums.Game.Rooms;
using Ada.Core.Enums.Miscellaneous;
using Ada.Db.Models.Constants;
using Ada.Game.Rooms.Mapping;
using Ada.Game.Rooms.PathFinding;
using Ada.Game.Rooms.Users;
using Ada.Networking.Writers.Rooms.Users;
using Moq;

namespace Ada.Tests.Game.Rooms.Users;

[TestFixture]
public class RoomUserEffectTests
{
    private static PlayerFurnitureItemPlacementDataDto WaterTileAt(int x, int y) => new()
    {
        PlayerFurnitureItem = new PlayerFurnitureItemDto
        {
            FurnitureItemId = 0,
            LimitedData = "",
            MetaData = "",
            FurnitureItem = new FurnitureItemDto
            {
                Name = "",
                AssetName = "",
                InteractionType = FurnitureItemInteractionType.Water,
                CanWalk = true
            }
        },
        PositionX = x,
        PositionY = y,
        Direction = HDirection.North
    };

    private static (RoomUser user, List<AbstractPacketWriter> broadcasts) CreateUserOn(
        List<PlayerFurnitureItemPlacementDataDto> furniture,
        Point point)
    {
        var roomDto = new RoomDto
        {
            FurnitureItems = furniture,
            Settings = new RoomSettingsDto()
        };

        var tileMap = new RoomTileMap("000\n000\n000", furniture);
        var broadcasts = new List<AbstractPacketWriter>();

        var room = new Mock<IRoomLogic>();
        room.SetupGet(x => x.Room).Returns(roomDto);
        room.SetupGet(x => x.TileMap).Returns(tileMap);
        room.Setup(x => x.BroadcastDataAsync(
                It.IsAny<AbstractPacketWriter>(),
                It.IsAny<IReadOnlyCollection<long>?>()))
            .Callback<AbstractPacketWriter, IReadOnlyCollection<long>?>((w, _) => broadcasts.Add(w))
            .Returns(Task.CompletedTask);

        var player = new Mock<IPlayerLogic>();
        player.SetupGet(x => x.Player).Returns(new PlayerDto(
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
            []));

        var user = new RoomUser(
            room.Object,
            new Mock<INetworkObject>().Object,
            point,
            0,
            HDirection.North,
            HDirection.North,
            player.Object,
            new ServerRoomConstants { SecondsTillUserIdle = 3600 },
            RoomControllerLevel.None,
            tileMap,
            new Mock<IRoomHelperService>().Object,
            new Mock<IRoomWiredService>().Object,
            new RoomPathFinderHelperService(),
            new Mock<IRoomFurnitureItemInteractorRepository>().Object);

        tileMap.AddUnitToMap(point, user);

        return (user, broadcasts);
    }

    [Test]
    public async Task RunPeriodicCheckAsync_StandingOnEffectTile_BroadcastsEffectOnlyOnce()
    {
        var (user, broadcasts) = CreateUserOn([WaterTileAt(0, 0)], new Point(0, 0));

        await user.RunPeriodicCheckAsync();
        await user.RunPeriodicCheckAsync();
        await user.RunPeriodicCheckAsync();

        Assert.That(broadcasts.OfType<RoomUserEffectWriter>().Count(), Is.EqualTo(1));
        Assert.That(user.ActiveEffectId, Is.EqualTo(29));
    }

    [Test]
    public async Task RunPeriodicCheckAsync_OffEffectTile_DoesNotBroadcast()
    {
        var (user, broadcasts) = CreateUserOn([WaterTileAt(2, 2)], new Point(0, 0));

        await user.RunPeriodicCheckAsync();
        await user.RunPeriodicCheckAsync();

        Assert.That(broadcasts.OfType<RoomUserEffectWriter>(), Is.Empty);
        Assert.That(user.ActiveEffectId, Is.Zero);
    }

    [Test]
    public async Task RunPeriodicCheckAsync_LeavingEffectTile_BroadcastsClearOnce()
    {
        var (user, broadcasts) = CreateUserOn([WaterTileAt(0, 0)], new Point(0, 0));

        await user.RunPeriodicCheckAsync();
        Assert.That(user.ActiveEffectId, Is.EqualTo(29));

        await user.SetPositionAsync(new Point(2, 2));

        await user.RunPeriodicCheckAsync();
        await user.RunPeriodicCheckAsync();

        var effects = broadcasts.OfType<RoomUserEffectWriter>().ToList();

        Assert.That(effects, Has.Count.EqualTo(2));
        Assert.That(effects[1].EffectId, Is.Zero);
        Assert.That(user.ActiveEffectId, Is.Zero);
    }
}
