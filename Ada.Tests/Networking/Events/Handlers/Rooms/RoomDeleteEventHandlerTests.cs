using Ada.API.DTOs.Furniture;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Networking.Client;
using Ada.Networking.Events.Handlers.Rooms.Furniture;
using Ada.Tests.Common;
using AutoMapper;
using Moq;

namespace Ada.Tests.Networking.Events.Handlers.Rooms;

[TestFixture]
public class RoomDeleteEventHandlerTests
{
    private const int RoomId = 10;
    private const long OwnerId = 1;
    private const long FurnitureOwnerId = 2;
    private const long OccupantId = 3;

    private SqliteTestDbFactory _dbFactory = null!;

    [SetUp]
    public void SetUp() => _dbFactory = new SqliteTestDbFactory();

    [TearDown]
    public void TearDown() => _dbFactory.Dispose();

    private static PlayerFurnitureItemPlacementDataDto Item(long ownerId) => new()
    {
        Id = 500,
        PlayerFurnitureItemId = 77,
        RoomId = RoomId,
        PlayerFurnitureItem = new PlayerFurnitureItemDto
        {
            Id = 77,
            PlayerId = ownerId,
            FurnitureItemId = 0,
            LimitedData = "",
            MetaData = "",
            FurnitureItem = new FurnitureItemDto { Name = "", AssetName = "", InteractionType = "" }
        }
    };

    [Test]
    public async Task Delete_NotifiesOnlineOwnersOfFurnitureLeftInTheRoom()
    {
        var furnitureOwner = new Mock<IPlayerLogic>();
        furnitureOwner.SetupGet(x => x.Player).Returns(TestPlayers.Minimal(FurnitureOwnerId, "owner2"));

        var players = new Mock<IPlayerRepository>();
        players.Setup(x => x.GetPlayerLogicById(FurnitureOwnerId)).Returns(furnitureOwner.Object);

        var helper = new Mock<IPlayerHelperService>();
        var (client, room) = Harness(players, out var users);

        room.SetupGet(x => x.Room).Returns(new RoomDto
        {
            Id = RoomId,
            OwnerId = OwnerId,
            FurnitureItems = [Item(FurnitureOwnerId)]
        });

        await Handler(players, helper).HandleAsync(client);

        helper.Verify(
            x => x.SendUnseenInventoryItemsAsync(
                furnitureOwner.Object,
                It.Is<List<PlayerFurnitureItemDto>>(items => items.Count == 1)),
            Times.Once,
            "the owner's items returned to their inventory but nothing told them");

        helper.Verify(x => x.RefreshInventoryAsync(furnitureOwner.Object), Times.Once);
        _ = users;
    }

    [Test]
    public async Task Delete_SendsOccupantsToTheHotelView()
    {
        var players = new Mock<IPlayerRepository>();
        var helper = new Mock<IPlayerHelperService>();
        var (client, room) = Harness(players, out var users);

        room.SetupGet(x => x.Room).Returns(new RoomDto { Id = RoomId, OwnerId = OwnerId });

        await Handler(players, helper).HandleAsync(client);

        users.Verify(
            x => x.TryRemoveAsync(OccupantId, It.IsAny<bool>(), true),
            Times.Once,
            "an occupant of a deleted room has nowhere to be unless they are sent to the hotel view");
    }

    private (INetworkClient Client, Mock<IRoomLogic> Room) Harness(
        Mock<IPlayerRepository> players,
        out Mock<IRoomUserRepository> users)
    {
        var occupant = new Mock<IPlayerLogic>();
        occupant.SetupGet(x => x.Player).Returns(TestPlayers.Minimal(OccupantId, "occupant"));

        var occupantUser = new Mock<IRoomUser>();
        occupantUser.SetupGet(x => x.Player).Returns(occupant.Object);

        users = new Mock<IRoomUserRepository>();
        users.Setup(x => x.GetAll()).Returns([occupantUser.Object]);
        users.Setup(x => x.TryRemoveAsync(It.IsAny<long>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        var room = new Mock<IRoomLogic>();
        room.SetupGet(x => x.UserRepository).Returns(users.Object);
        room.SetupGet(x => x.Room).Returns(new RoomDto { Id = RoomId, OwnerId = OwnerId });

        _rooms = new Mock<IRoomRepository>();
        _rooms.Setup(x => x.TryGetRoomById(RoomId)).Returns(room.Object);
        _rooms.Setup(x => x.TryRemove(RoomId, out It.Ref<IRoomLogic?>.IsAny)).Returns(true);

        var owner = new Mock<IPlayerLogic>();
        owner.SetupGet(x => x.Player).Returns(TestPlayers.Minimal(OwnerId, "owner"));

        var client = new Mock<INetworkClient>();
        client.SetupGet(x => x.Player).Returns(owner.Object);

        return (client.Object, room);
    }

    private Mock<IRoomRepository> _rooms = null!;

    private RoomDeleteEventHandler Handler(Mock<IPlayerRepository> players, Mock<IPlayerHelperService> helper) =>
        new(
            _rooms.Object,
            _dbFactory,
            Mock.Of<IMapper>(),
            players.Object,
            helper.Object,
            NullLogger<RoomDeleteEventHandler>.Instance)
        {
            RoomId = RoomId
        };
}
