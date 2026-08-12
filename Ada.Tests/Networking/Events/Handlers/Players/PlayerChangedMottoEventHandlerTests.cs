using Ada.API.Interfaces.Networking;
using Ada.API.DTOs.Players;
using Ada.API.DTOs.Rooms;
using Ada.API.DTOs.Server;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Game.WordFilter;
using Ada.API.Interfaces.Networking.Client;
using Ada.Core.Enums.Game.WordFilter;
using Ada.Db.Models.Constants;
using Ada.Networking.Events.Handlers.Players;
using Ada.Tests.Common;
using Moq;

namespace Ada.Tests.Networking.Events.Handlers.Players;

[TestFixture]
public class PlayerChangedMottoEventHandlerTests
{
    private const long _playerId = 1;

    private SqliteTestDbFactory _dbFactory = null!;

    [SetUp]
    public void SetUp() => _dbFactory = new SqliteTestDbFactory();

    [TearDown]
    public void TearDown() => _dbFactory.Dispose();

    private sealed record Harness(
        PlayerChangedMottoEventHandler Handler,
        INetworkClient Client,
        PlayerAvatarDataDto AvatarData,
        List<string> MottoAtBroadcast);

    private Harness Create(string motto, WordFilterResultDto filterResult)
    {
        var avatarData = new PlayerAvatarDataDto { FigureCode = "hd-180-1", Motto = "old motto" };

        var player = new Mock<IPlayerLogic>();
        player.SetupGet(x => x.Player).Returns(MakePlayerDto(avatarData));

        var state = new Mock<IPlayerState>();
        state.SetupAllProperties();
        state.Object.CurrentRoomId = 10;
        player.SetupGet(x => x.State).Returns(state.Object);

        var roomUser = new Mock<IRoomUser>();
        roomUser.SetupGet(x => x.Player).Returns(player.Object);

        var mottoAtBroadcast = new List<string>();

        var room = new Mock<IRoomLogic>();
        room.SetupGet(x => x.Room).Returns(new RoomDto { Id = 10 });
        room.Setup(x => x.BroadcastDataAsync(It.IsAny<AbstractPacketWriter>(), It.IsAny<IReadOnlyCollection<long>>()))
            .Callback(() => mottoAtBroadcast.Add(avatarData.Motto))
            .Returns(Task.CompletedTask);
        room.Setup(x => x.UserRepository.TryGetById(_playerId, out It.Ref<IRoomUser?>.IsAny))
            .Returns((long _, out IRoomUser? u) => { u = roomUser.Object; return true; });

        var roomRepository = new Mock<IRoomRepository>();
        roomRepository.Setup(x => x.TryGetRoomById(10)).Returns(room.Object);

        var wordFilter = new Mock<IWordFilterService>();
        wordFilter.Setup(x => x.Filter(It.IsAny<string>(), It.IsAny<WordFilterContext>()))
            .Returns(filterResult);

        var client = new Mock<INetworkClient>();
        client.SetupGet(x => x.Player).Returns(player.Object);
        client.SetupGet(x => x.RoomUser).Returns(roomUser.Object);

        var handler = new PlayerChangedMottoEventHandler(
            roomRepository.Object,
            new ServerPlayerConstants { MaxMottoLength = 38 },
            wordFilter.Object,
            _dbFactory)
        {
            Motto = motto
        };

        return new Harness(handler, client.Object, avatarData, mottoAtBroadcast);
    }

    [Test]
    public async Task Handle_BroadcastsTheNewMottoNotTheOldOne()
    {
        var harness = Create("new motto", Passthrough("new motto"));

        await harness.Handler.HandleAsync(harness.Client);

        Assert.Multiple(() =>
        {
            Assert.That(harness.AvatarData.Motto, Is.EqualTo("new motto"));
            Assert.That(harness.MottoAtBroadcast, Is.EqualTo(new[] { "new motto" }),
                "broadcasting before applying sent everyone the previous motto");
        });
    }

    [Test]
    public async Task Handle_BlockedMotto_IsNotAppliedOrBroadcast()
    {
        var harness = Create("bad words", new WordFilterResultDto
        {
            OriginalText = "bad words",
            FilteredText = "bad words",
            Action = WordFilterAction.Block
        });

        await harness.Handler.HandleAsync(harness.Client);

        Assert.Multiple(() =>
        {
            Assert.That(harness.AvatarData.Motto, Is.EqualTo("old motto"));
            Assert.That(harness.MottoAtBroadcast, Is.Empty);
        });
    }

    [Test]
    public async Task Handle_FilteredMotto_StoresTheFilteredText()
    {
        var harness = Create("rude", Passthrough("****"));

        await harness.Handler.HandleAsync(harness.Client);

        Assert.That(harness.AvatarData.Motto, Is.EqualTo("****"));
    }

    private static WordFilterResultDto Passthrough(string text) =>
        new() { OriginalText = text, FilteredText = text };

    private static PlayerDto MakePlayerDto(PlayerAvatarDataDto avatarData) => new(
        _playerId, "player", "", DateTimeOffset.UtcNow, [], new PlayerDataDto(), avatarData, [], [], [], [],
        new PlayerNavigatorSettingsDto(), new PlayerGameSettingsDto(), [], [], [], [], [], [], [], [], [], [], [],
        [], [], [], [], [], []);
}
