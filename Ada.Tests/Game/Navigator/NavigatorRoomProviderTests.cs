using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Bots;
using Ada.API.Interfaces.Game.Rooms.Pets;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Db.Models;
using Ada.Db.Models.Players;
using Ada.Db.Models.Rooms;
using Ada.Game.Mappers;
using Ada.Game.Navigator;
using Ada.Game.Navigator.Filterers;
using Ada.Tests.Common;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Ada.Tests.Game.Navigator;

[TestFixture]
public class NavigatorRoomProviderTests
{
    private static readonly DateTimeOffset BaseTime = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private SqliteTestDbFactory _factory = null!;

    [SetUp]
    public void SetUp() => _factory = new SqliteTestDbFactory();

    [TearDown]
    public void TearDown() => _factory.Dispose();

    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection()
            .AddSingleton(Mock.Of<IRoomUserRepository>())
            .AddSingleton(Mock.Of<IRoomBotRepository>())
            .AddSingleton(Mock.Of<IRoomPetRepository>())
            .AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(
                Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        MapperServiceCollection.AddServices(services);

        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static IPlayerLogic CreatePlayer(long id)
    {
        var playerData = new PlayerDto(
            id,
            "TestUser",
            "test@example.com",
            BaseTime,
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
        return player.Object;
    }

    private NavigatorRoomProvider CreateProvider(IRoomRepository? roomRepository = null)
    {
        return new NavigatorRoomProvider(
            roomRepository ?? Mock.Of<IRoomRepository>(),
            _factory,
            [new TagFilterer(), new OwnerFilterer(), new RoomNameFilterer()],
            CreateMapper());
    }

    private async Task SeedAsync()
    {
        await using var db = _factory.CreateDbContext();

        db.RoomLayouts.Add(new RoomLayout { Id = 1 });

        db.Players.Add(new Player { Id = 1, Username = "alice", Email = "a@test.com", Password = "secret" });
        db.Players.Add(new Player
        {
            Id = 2, Username = "bob", Email = "b@test.com", Password = "secret",
            Roles = { new Role { Id = 1, Name = "Admin" } }
        });
        db.Players.Add(new Player { Id = 3, Username = "carol", Email = "c@test.com", Password = "secret" });

        db.Rooms.Add(new Room
        {
            Id = 1, Name = "Alpha Room", Description = "first", OwnerId = 1, LayoutId = 1,
            CreatedAt = BaseTime, Tags = { new RoomTag { Name = "cool" } }
        });
        db.Rooms.Add(new Room
        {
            Id = 2, Name = "Beta", Description = "second place", OwnerId = 2, LayoutId = 1,
            CreatedAt = BaseTime.AddMinutes(1)
        });
        db.Rooms.Add(new Room
        {
            Id = 3, Name = "Gamma", Description = "third", OwnerId = 3, LayoutId = 1,
            CreatedAt = BaseTime.AddMinutes(2)
        });

        db.PlayerRoomLikes.Add(new PlayerRoomLike { Id = 1, PlayerId = 1, RoomId = 2 });
        db.PlayerRoomLikes.Add(new PlayerRoomLike { Id = 2, PlayerId = 3, RoomId = 2 });
        db.PlayerRoomLikes.Add(new PlayerRoomLike { Id = 3, PlayerId = 1, RoomId = 3 });

        await db.SaveChangesAsync();
    }

    [Test]
    public async Task GetRoomsForCategoryNameAsync_UnknownCategory_ReturnsEmpty()
    {
        await SeedAsync();
        var provider = CreateProvider();

        var rooms = await provider.GetRoomsForCategoryNameAsync(CreatePlayer(1), "unknown");

        Assert.That(rooms, Is.Empty);
    }

    [Test]
    public async Task GetRoomsForCategoryNameAsync_MyRooms_ReturnsOwnedRoomsOnly()
    {
        await SeedAsync();
        var provider = CreateProvider();

        var rooms = await provider.GetRoomsForCategoryNameAsync(CreatePlayer(1), "my_rooms");

        Assert.That(rooms.Select(x => x.Id), Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public async Task GetRoomsForCategoryNameAsync_Popular_OrdersByOnlineUsersThenLikes()
    {
        await SeedAsync();

        var userRepository = new Mock<IRoomUserRepository>();
        userRepository.SetupGet(x => x.Count).Returns(5);
        var roomLogic = new Mock<IRoomLogic>();
        roomLogic.SetupGet(x => x.UserRepository).Returns(userRepository.Object);
        var roomRepository = new Mock<IRoomRepository>();
        roomRepository.Setup(x => x.TryGetRoomById(3)).Returns(roomLogic.Object);

        var provider = CreateProvider(roomRepository.Object);

        var rooms = await provider.GetRoomsForCategoryNameAsync(CreatePlayer(1), "popular");

        Assert.That(rooms.Select(x => x.Id), Is.EqualTo(new[] { 3, 2, 1 }));
    }

    [Test]
    public async Task GetRoomsForCategoryNameAsync_Official_ReturnsStaffOwnedRoomsOnly()
    {
        await SeedAsync();
        var provider = CreateProvider();

        var rooms = await provider.GetRoomsForCategoryNameAsync(CreatePlayer(1), "official");

        Assert.That(rooms.Select(x => x.Id), Is.EqualTo(new[] { 2 }));
    }

    [TestCase("Alpha", 1)]
    [TestCase("second", 2)]
    [TestCase("cool", 1)]
    [TestCase("carol", 3)]
    public async Task GetRoomsForSearchQueryAsync_PlainQuery_MatchesNameDescriptionTagOrOwner(
        string searchQuery, int expectedRoomId)
    {
        await SeedAsync();
        var provider = CreateProvider();

        var rooms = await provider.GetRoomsForSearchQueryAsync(searchQuery);

        Assert.That(rooms.Select(x => x.Id), Is.EqualTo(new[] { expectedRoomId }));
    }

    [Test]
    public async Task GetRoomsForSearchQueryAsync_KnownFilter_AppliesFilterer()
    {
        await SeedAsync();
        var provider = CreateProvider();

        var rooms = await provider.GetRoomsForSearchQueryAsync("tag:cool");

        Assert.That(rooms.Select(x => x.Id), Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public async Task GetRoomsForSearchQueryAsync_UnknownFilter_ReturnsUnfiltered()
    {
        await SeedAsync();
        var provider = CreateProvider();

        var rooms = await provider.GetRoomsForSearchQueryAsync("bogus:anything");

        Assert.That(rooms, Has.Count.EqualTo(3));
    }
}
