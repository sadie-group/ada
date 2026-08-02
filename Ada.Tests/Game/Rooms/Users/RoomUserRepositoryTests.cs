using Ada.API;
using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Bots;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Packets;
using Ada.Game.Rooms.Users;
using Ada.Networking.Packets;
using Ada.Networking.Writers.Rooms;
using Ada.Networking.Writers.Rooms.Bots;
using Ada.Networking.Writers.Rooms.Users;
using Microsoft.Extensions.Logging;
using Moq;

namespace Ada.Tests.Game.Rooms.Users;

[TestFixture]
public class RoomUserRepositoryTests
{
    private sealed class FixedIdMap : IPacketIdMap
    {
        public bool TryGetHandlerType(short packetId, out Type? handlerType)
        {
            handlerType = null;
            return false;
        }

        public bool TryGetOutgoingId(Type writerType, out short packetId)
        {
            packetId = 1;
            return true;
        }
    }

    private sealed class TestCodec : IPacketCodec
    {
        public string Revision => "PRODUCTION";

        public IPacketIdMap IdMap { get; } = new FixedIdMap();

        public INetworkPacketDecoder Decoder { get; } = new NetworkPacketDecoder();

        public INetworkPacketWriter CreateWriter() => new NetworkPacketWriter();

        public INetworkPacketReader CreateReader(ReadOnlyMemory<byte> body) => new NetworkPacketReader(body);
    }

    private sealed class TestRoomUser
    {
        public required Mock<IRoomUser> User { get; init; }
        public required Mock<IPlayerLogic> Player { get; init; }
        public required Mock<IPlayerState> State { get; init; }
        public required Mock<INetworkObject> NetworkObject { get; init; }
        public required List<PlayerFriendshipDto> Friendships { get; init; }
    }

    private static readonly TestCodec Codec = new();

    private static PlayerDto MakePlayerDto(long id, string username)
    {
        return new PlayerDto(
            id,
            username,
            "test@example.com",
            DateTimeOffset.UtcNow,
            [],
            new PlayerDataDto(),
            new PlayerAvatarDataDto { FigureCode = "figure", Motto = "motto" },
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
    }

    private static TestRoomUser MakeUser(long id, string username = "user")
    {
        var state = new Mock<IPlayerState>();
        state.SetupAllProperties();

        var friendships = new List<PlayerFriendshipDto>();

        var player = new Mock<IPlayerLogic>();
        player.SetupGet(x => x.Player).Returns(MakePlayerDto(id, username));
        player.SetupGet(x => x.State).Returns(state.Object);
        player.Setup(x => x.GetMergedFriendships()).Returns(friendships);

        var networkObject = new Mock<INetworkObject>();
        networkObject.SetupGet(x => x.Codec).Returns(Codec);

        var user = new Mock<IRoomUser>();
        user.SetupAllProperties();
        user.SetupGet(x => x.Player).Returns(player.Object);
        user.SetupGet(x => x.NetworkObject).Returns(networkObject.Object);
        user.SetupGet(x => x.StatusMap).Returns(new Dictionary<string, string>());

        return new TestRoomUser
        {
            User = user,
            Player = player,
            State = state,
            NetworkObject = networkObject,
            Friendships = friendships
        };
    }

    private static (RoomUserRepository Repository, Mock<IRoomLogic> Room, Mock<IPlayerHelperService> Helper, Mock<IPlayerRepository> Players) MakeRepository()
    {
        var helper = new Mock<IPlayerHelperService>();
        var players = new Mock<IPlayerRepository>();

        var repository = new RoomUserRepository(
            Mock.Of<ILogger<RoomUserRepository>>(),
            players.Object,
            helper.Object);

        var room = new Mock<IRoomLogic>();
        repository.SetRoom(room.Object);

        return (repository, room, helper, players);
    }

    [Test]
    public void TryAdd_NewUser_AddsAndAppearsInSnapshot()
    {
        var (repository, _, _, _) = MakeRepository();
        var user = MakeUser(1);

        Assert.That(repository.TryAdd(user.User.Object), Is.True);
        Assert.That(repository.Count, Is.EqualTo(1));
        Assert.That(repository.GetAll(), Does.Contain(user.User.Object));
    }

    [Test]
    public void TryAdd_DuplicateId_ReturnsFalse()
    {
        var (repository, _, _, _) = MakeRepository();

        Assert.That(repository.TryAdd(MakeUser(1).User.Object), Is.True);
        Assert.That(repository.TryAdd(MakeUser(1).User.Object), Is.False);
        Assert.That(repository.Count, Is.EqualTo(1));
    }

    [Test]
    public void TryGetById_Present_ReturnsUser()
    {
        var (repository, _, _, _) = MakeRepository();
        var user = MakeUser(7);
        repository.TryAdd(user.User.Object);

        Assert.That(repository.TryGetById(7, out var found), Is.True);
        Assert.That(found, Is.SameAs(user.User.Object));
    }

    [Test]
    public void TryGetById_Absent_ReturnsFalse()
    {
        var (repository, _, _, _) = MakeRepository();

        Assert.That(repository.TryGetById(42, out var found), Is.False);
        Assert.That(found, Is.Null);
    }

    [Test]
    public void TryGetByUsername_Present_ReturnsUser()
    {
        var (repository, _, _, _) = MakeRepository();
        var user = MakeUser(1, "Alice");
        repository.TryAdd(user.User.Object);

        Assert.That(repository.TryGetByUsername("Alice", out var found), Is.True);
        Assert.That(found, Is.SameAs(user.User.Object));
    }

    [Test]
    public void TryGetByUsername_Absent_ReturnsFalse()
    {
        var (repository, _, _, _) = MakeRepository();
        repository.TryAdd(MakeUser(1, "Alice").User.Object);

        Assert.That(repository.TryGetByUsername("Bob", out var found), Is.False);
        Assert.That(found, Is.Null);
    }

    [Test]
    public void GetAllWithRights_MixedUsers_ReturnsOnlyUsersWithRights()
    {
        var (repository, _, _, _) = MakeRepository();

        var withRights = MakeUser(1);
        withRights.User.Setup(x => x.HasRights()).Returns(true);

        var withoutRights = MakeUser(2);
        withoutRights.User.Setup(x => x.HasRights()).Returns(false);

        repository.TryAdd(withRights.User.Object);
        repository.TryAdd(withoutRights.User.Object);

        Assert.That(repository.GetAllWithRights(), Is.EqualTo(new[] { withRights.User.Object }));
    }

    [Test]
    public async Task TryRemoveAsync_UnknownId_LeavesRepositoryUntouched()
    {
        var (repository, _, helper, _) = MakeRepository();
        repository.TryAdd(MakeUser(1).User.Object);

        await repository.TryRemoveAsync(99, true);

        Assert.That(repository.Count, Is.EqualTo(1));
        helper.Verify(x => x.UpdatePlayerStatusForFriendsAsync(
            It.IsAny<IPlayerLogic>(),
            It.IsAny<IEnumerable<PlayerFriendshipDto>>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<IPlayerRepository>()), Times.Never);
    }

    [Test]
    public async Task TryRemoveAsync_NotifyLeft_BroadcastsUserLeft()
    {
        var (repository, room, _, _) = MakeRepository();
        var user = MakeUser(5);
        repository.TryAdd(user.User.Object);

        await repository.TryRemoveAsync(5, true);

        Assert.That(repository.Count, Is.Zero);
        Assert.That(repository.GetAll(), Is.Empty);
        room.Verify(x => x.BroadcastDataAsync(
            It.Is<AbstractPacketWriter>(w => ((RoomUserLeftWriter)w).UserId == "5"),
            It.IsAny<IReadOnlyCollection<long>?>()), Times.Once);
    }

    [Test]
    public async Task TryRemoveAsync_RemovedUser_ClearsRoomStateAndNotifiesFriends()
    {
        var (repository, _, helper, players) = MakeRepository();
        var user = MakeUser(5);
        user.State.Object.CurrentRoomId = 123;
        repository.TryAdd(user.User.Object);

        await repository.TryRemoveAsync(5, false);

        Assert.That(user.State.Object.CurrentRoomId, Is.Zero);
        helper.Verify(x => x.UpdatePlayerStatusForFriendsAsync(
            user.Player.Object,
            user.Friendships,
            true,
            false,
            players.Object), Times.Once);
        user.User.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Test]
    public async Task TryRemoveAsync_HotelView_SendsHotelViewPacket()
    {
        var (repository, _, _, _) = MakeRepository();
        var user = MakeUser(5);
        repository.TryAdd(user.User.Object);

        await repository.TryRemoveAsync(5, false, hotelView: true);

        user.NetworkObject.Verify(
            x => x.WriteToStreamAsync(It.Is<AbstractPacketWriter>(w => w is RoomUserHotelViewWriter)),
            Times.Once);
    }

    [Test]
    public async Task ProcessNewWalkRequestsAsync_NoWalkers_QueuesNothing()
    {
        var (repository, _, _, _) = MakeRepository();
        var user = MakeUser(1);
        user.User.Setup(x => x.TryStartPendingWalkAsync()).ReturnsAsync(false);
        repository.TryAdd(user.User.Object);

        await repository.ProcessNewWalkRequestsAsync();

        user.NetworkObject.Verify(x => x.QueueOutbound(It.IsAny<INetworkPacketWriter>()), Times.Never);
    }

    [Test]
    public async Task ProcessNewWalkRequestsAsync_WalkStarted_QueuesDataAndStatusToAllUsers()
    {
        var (repository, _, _, _) = MakeRepository();

        var walker = MakeUser(1, "walker");
        walker.User.Setup(x => x.TryStartPendingWalkAsync()).ReturnsAsync(true);
        walker.User.Object.NeedsUpdate = true;

        var bystander = MakeUser(2, "bystander");
        bystander.User.Setup(x => x.TryStartPendingWalkAsync()).ReturnsAsync(false);

        repository.TryAdd(walker.User.Object);
        repository.TryAdd(bystander.User.Object);

        await repository.ProcessNewWalkRequestsAsync();

        walker.NetworkObject.Verify(x => x.QueueOutbound(It.IsAny<INetworkPacketWriter>()), Times.Exactly(2));
        bystander.NetworkObject.Verify(x => x.QueueOutbound(It.IsAny<INetworkPacketWriter>()), Times.Exactly(2));
        Assert.That(walker.User.Object.NeedsUpdate, Is.False);
    }

    [Test]
    public void ProcessNewWalkRequestsAsync_WalkCheckThrows_SwallowsException()
    {
        var (repository, _, _, _) = MakeRepository();
        var user = MakeUser(1);
        user.User.Setup(x => x.TryStartPendingWalkAsync()).ThrowsAsync(new InvalidOperationException("boom"));
        repository.TryAdd(user.User.Object);

        Assert.DoesNotThrowAsync(() => repository.ProcessNewWalkRequestsAsync());
    }

    [Test]
    public async Task RunPeriodicCheckAsync_NoUsers_TracksNoUsersSince()
    {
        var (repository, _, _, _) = MakeRepository();

        await repository.RunPeriodicCheckAsync();

        Assert.That(repository.NoUsersSince, Is.Not.Null);
        var first = repository.NoUsersSince;

        await repository.RunPeriodicCheckAsync();

        Assert.That(repository.NoUsersSince, Is.EqualTo(first));
    }

    [Test]
    public async Task RunPeriodicCheckAsync_UsersPresent_ResetsNoUsersSinceAndChecksUsers()
    {
        var (repository, room, _, _) = MakeRepository();
        repository.NoUsersSince = DateTime.UtcNow;

        var botRepository = new Mock<IRoomBotRepository>();
        botRepository.SetupGet(x => x.Count).Returns(0);
        room.SetupGet(x => x.BotRepository).Returns(botRepository.Object);

        var user = MakeUser(1);
        repository.TryAdd(user.User.Object);

        await repository.RunPeriodicCheckAsync();

        Assert.That(repository.NoUsersSince, Is.Null);
        user.User.Verify(x => x.RunPeriodicCheckAsync(), Times.Once);
    }

    [Test]
    public async Task RunPeriodicCheckAsync_BotsNeedUpdate_BroadcastsBotPackets()
    {
        var (repository, room, _, _) = MakeRepository();
        repository.TryAdd(MakeUser(1).User.Object);

        var bot = new Mock<IRoomBot>();
        bot.SetupAllProperties();
        bot.Object.NeedsUpdate = true;

        var idleBot = new Mock<IRoomBot>();
        idleBot.SetupAllProperties();

        var botRepository = new Mock<IRoomBotRepository>();
        botRepository.SetupGet(x => x.Count).Returns(2);
        botRepository.Setup(x => x.GetAll()).Returns([bot.Object, idleBot.Object]);
        room.SetupGet(x => x.BotRepository).Returns(botRepository.Object);

        await repository.RunPeriodicCheckAsync();

        room.Verify(x => x.BroadcastDataAsync(
            It.Is<AbstractPacketWriter>(w => w is RoomBotStatusWriter),
            It.IsAny<IReadOnlyCollection<long>?>()), Times.Once);
        room.Verify(x => x.BroadcastDataAsync(
            It.Is<AbstractPacketWriter>(w => w is RoomBotDataWriter),
            It.IsAny<IReadOnlyCollection<long>?>()), Times.Once);
        Assert.That(bot.Object.NeedsUpdate, Is.False);
    }

    [Test]
    public async Task RunPeriodicCheckAsync_NoBotsNeedUpdate_DoesNotBroadcast()
    {
        var (repository, room, _, _) = MakeRepository();
        repository.TryAdd(MakeUser(1).User.Object);

        var idleBot = new Mock<IRoomBot>();
        idleBot.SetupAllProperties();

        var botRepository = new Mock<IRoomBotRepository>();
        botRepository.SetupGet(x => x.Count).Returns(1);
        botRepository.Setup(x => x.GetAll()).Returns([idleBot.Object]);
        room.SetupGet(x => x.BotRepository).Returns(botRepository.Object);

        await repository.RunPeriodicCheckAsync();

        room.Verify(x => x.BroadcastDataAsync(
            It.IsAny<AbstractPacketWriter>(),
            It.IsAny<IReadOnlyCollection<long>?>()), Times.Never);
    }

    [Test]
    public async Task RunPeriodicCheckAsync_UsersNeedUpdate_QueuesDataAndStatus()
    {
        var (repository, room, _, _) = MakeRepository();

        var botRepository = new Mock<IRoomBotRepository>();
        botRepository.SetupGet(x => x.Count).Returns(0);
        room.SetupGet(x => x.BotRepository).Returns(botRepository.Object);

        var user = MakeUser(1);
        user.User.Object.NeedsUpdate = true;
        repository.TryAdd(user.User.Object);

        await repository.RunPeriodicCheckAsync();

        user.NetworkObject.Verify(x => x.QueueOutbound(It.IsAny<INetworkPacketWriter>()), Times.Exactly(2));
        Assert.That(user.User.Object.NeedsUpdate, Is.False);
    }

    [Test]
    public void RunPeriodicCheckAsync_UserCheckThrows_SwallowsException()
    {
        var (repository, _, _, _) = MakeRepository();
        var user = MakeUser(1);
        user.User.Setup(x => x.RunPeriodicCheckAsync()).ThrowsAsync(new InvalidOperationException("boom"));
        repository.TryAdd(user.User.Object);

        Assert.DoesNotThrowAsync(() => repository.RunPeriodicCheckAsync());
    }

    [Test]
    public async Task DisposeAsync_WithUsers_RemovesAndDisposesAll()
    {
        var (repository, room, _, _) = MakeRepository();
        var first = MakeUser(1);
        var second = MakeUser(2);
        repository.TryAdd(first.User.Object);
        repository.TryAdd(second.User.Object);

        await repository.DisposeAsync();

        Assert.That(repository.Count, Is.Zero);
        first.User.Verify(x => x.DisposeAsync(), Times.Once);
        second.User.Verify(x => x.DisposeAsync(), Times.Once);
        room.Verify(x => x.BroadcastDataAsync(
            It.IsAny<AbstractPacketWriter>(),
            It.IsAny<IReadOnlyCollection<long>?>()), Times.Never);
    }
}
