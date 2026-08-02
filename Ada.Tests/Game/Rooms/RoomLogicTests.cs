using Ada.API;
using Ada.API.DTOs.Players;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms.Bots;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Pathfinding;
using Ada.API.Interfaces.Game.Rooms.Pets;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Packets;
using Ada.Game.Rooms;
using Ada.Networking.Packets;
using Moq;

namespace Ada.Tests.Game.Rooms;

[TestFixture]
public class RoomLogicTests
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

    private sealed class StubPacketWriter : AbstractPacketWriter;

    private static readonly TestCodec Codec = new();

    private static PlayerDto MakePlayerDto(long id)
    {
        return new PlayerDto(
            id,
            $"user{id}",
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
    }

    private static (Mock<IRoomUser> User, Mock<INetworkObject> NetworkObject) MakeUser(long id)
    {
        var player = new Mock<IPlayerLogic>();
        player.SetupGet(x => x.Player).Returns(MakePlayerDto(id));

        var networkObject = new Mock<INetworkObject>();
        networkObject.SetupGet(x => x.Codec).Returns(Codec);

        var user = new Mock<IRoomUser>();
        user.SetupGet(x => x.Player).Returns(player.Object);
        user.SetupGet(x => x.NetworkObject).Returns(networkObject.Object);

        return (user, networkObject);
    }

    private static RoomLogic CreateLogic(params Mock<IRoomUser>[] users)
    {
        var userRepository = new Mock<IRoomUserRepository>();
        userRepository.Setup(x => x.GetAll()).Returns(users.Select(x => x.Object).ToList());

        return new RoomLogic(
            new RoomDto(),
            Mock.Of<IRoomTileMap>(),
            Mock.Of<IRoomPathFinder>(),
            userRepository.Object,
            Mock.Of<IRoomBotRepository>(),
            Mock.Of<IRoomPetRepository>())
        {
            Name = "",
            Description = ""
        };
    }

    [Test]
    public async Task RunLockedAsync_RunsAction()
    {
        var logic = CreateLogic();
        var ran = false;

        await logic.RunLockedAsync(() =>
        {
            ran = true;
            return Task.CompletedTask;
        });

        Assert.That(ran, Is.True);
    }

    [Test]
    public async Task RunLockedAsync_Reentrant_RunsInlineWithoutDeadlock()
    {
        var logic = CreateLogic();
        var innerRan = false;

        await logic.RunLockedAsync(async () =>
        {
            await logic.RunLockedAsync(() =>
            {
                innerRan = true;
                return Task.CompletedTask;
            });
        });

        Assert.That(innerRan, Is.True);
    }

    [Test]
    public async Task RunLockedAsync_AfterCompletion_LockIsReleased()
    {
        var logic = CreateLogic();
        var runs = 0;

        await logic.RunLockedAsync(() =>
        {
            runs++;
            return Task.CompletedTask;
        });
        await logic.RunLockedAsync(() =>
        {
            runs++;
            return Task.CompletedTask;
        });

        Assert.That(runs, Is.EqualTo(2));
    }

    [Test]
    public void DisposeAsync_Completes()
    {
        var logic = CreateLogic();

        Assert.DoesNotThrowAsync(async () => await logic.DisposeAsync());
    }

    [Test]
    public void BroadcastDataAsync_NoUsers_Completes()
    {
        var logic = CreateLogic();

        Assert.DoesNotThrowAsync(() => logic.BroadcastDataAsync(new StubPacketWriter()));
    }

    [Test]
    public async Task BroadcastDataAsync_QueuesToEveryUser()
    {
        var (first, firstNetwork) = MakeUser(1);
        var (second, secondNetwork) = MakeUser(2);
        var logic = CreateLogic(first, second);

        await logic.BroadcastDataAsync(new StubPacketWriter());

        firstNetwork.Verify(x => x.QueueOutbound(It.IsAny<INetworkPacketWriter>()), Times.Once);
        secondNetwork.Verify(x => x.QueueOutbound(It.IsAny<INetworkPacketWriter>()), Times.Once);
    }

    [Test]
    public async Task BroadcastDataAsync_ExcludedIdList_SkipsExcludedUser()
    {
        var (first, firstNetwork) = MakeUser(1);
        var (second, secondNetwork) = MakeUser(2);
        var logic = CreateLogic(first, second);

        await logic.BroadcastDataAsync(new StubPacketWriter(), new List<long> { 1 });

        firstNetwork.Verify(x => x.QueueOutbound(It.IsAny<INetworkPacketWriter>()), Times.Never);
        secondNetwork.Verify(x => x.QueueOutbound(It.IsAny<INetworkPacketWriter>()), Times.Once);
    }

    [Test]
    public async Task BroadcastDataAsync_ExcludedIdSet_SkipsExcludedUser()
    {
        var (first, firstNetwork) = MakeUser(1);
        var (second, secondNetwork) = MakeUser(2);
        var logic = CreateLogic(first, second);

        await logic.BroadcastDataAsync(new StubPacketWriter(), new HashSet<long> { 2 });

        firstNetwork.Verify(x => x.QueueOutbound(It.IsAny<INetworkPacketWriter>()), Times.Once);
        secondNetwork.Verify(x => x.QueueOutbound(It.IsAny<INetworkPacketWriter>()), Times.Never);
    }

    [Test]
    public async Task BroadcastDataAsync_EmptyExclusions_QueuesToEveryone()
    {
        var (first, firstNetwork) = MakeUser(1);
        var logic = CreateLogic(first);

        await logic.BroadcastDataAsync(new StubPacketWriter(), Array.Empty<long>());

        firstNetwork.Verify(x => x.QueueOutbound(It.IsAny<INetworkPacketWriter>()), Times.Once);
    }
}
