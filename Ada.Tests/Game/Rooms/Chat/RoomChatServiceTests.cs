using Ada.API.DTOs.Players;
using Ada.API.DTOs.Rooms;
using Ada.API.DTOs.Server;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Chat.Commands;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Game.WordFilter;
using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Client;
using Ada.Core.Enums.Game.Rooms;
using Ada.Core.Enums.Game.Rooms.Users;
using Ada.Core.Enums.Game.WordFilter;
using Ada.Core.Enums.Miscellaneous;
using Ada.Db.Models.Constants;
using Ada.Game.Rooms.Chat;
using Ada.Networking.Writers.Rooms.Users;
using Ada.Networking.Writers.Rooms.Users.Chat;
using Moq;

namespace Ada.Tests.Game.Rooms.Chat;

[TestFixture]
public class RoomChatServiceTests
{
    private const long _senderId = 7;
    private const int _roomId = 42;

    private sealed class Harness
    {
        public Mock<INetworkClient> Client { get; } = new();
        public Mock<IRoomLogic> Room { get; } = new();
        public Mock<IRoomUser> RoomUser { get; } = new();
        public Mock<INetworkObject> SenderSocket { get; } = new();
        public Mock<IRoomRepository> Rooms { get; } = new();
        public Mock<IRoomChatCommandRepository> Commands { get; } = new();
        public Mock<IRoomWiredService> Wired { get; } = new();
        public Mock<IRoomHelperService> RoomHelper { get; } = new();
        public Mock<IWordFilterService> WordFilter { get; } = new();
        public Mock<IRoomFloodProtectionService> Flood { get; } = new();
        public Mock<IRoomWordFilterService> RoomWordFilter { get; } = new();
        public RoomDto RoomDto { get; } = new() { Id = _roomId, OwnerId = 1 };

        public List<AbstractPacketWriter> Broadcasts { get; } = [];
        public List<IReadOnlyCollection<long>?> BroadcastExclusions { get; } = [];
        public List<AbstractPacketWriter> ToSender { get; } = [];

        public ServerRoomConstants Constants { get; } = new() { MaxChatMessageLength = 100 };

        public Task RunAsync(string message, bool shouting = false, ChatBubble bubble = ChatBubble.Default)
            => RoomChatService.OnChatMessageAsync(
                Client.Object,
                message,
                shouting,
                Constants,
                Rooms.Object,
                Commands.Object,
                bubble,
                Wired.Object,
                RoomHelper.Object,
                WordFilter.Object,
                Flood.Object,
                RoomWordFilter.Object);
    }

    private static PlayerDto Player(long id, params long[] ignoring)
        => new(
            id, $"user{id}", $"user{id}@example.test", DateTimeOffset.UtcNow,
            [], new PlayerDataDto(), new PlayerAvatarDataDto(), [], [], [], [],
            new PlayerNavigatorSettingsDto(), new PlayerGameSettingsDto(),
            [], [], [], [], [], [], [], [],
            ignoring.Select(target => new PlayerIgnoreDto { TargetPlayerId = target }).ToList(),
            [], [], [], [], [], [], [], []);

    private static IRoomUser Bystander(long id, params long[] ignoring)
    {
        var logic = new Mock<IPlayerLogic>();
        logic.SetupGet(x => x.Player).Returns(Player(id, ignoring));

        var user = new Mock<IRoomUser>();
        user.SetupGet(x => x.Player).Returns(logic.Object);

        return user.Object;
    }

    private static Harness Build(bool hasRights = false, params IRoomUser[] bystanders)
    {
        var h = new Harness();

        var playerLogic = new Mock<IPlayerLogic>();
        playerLogic.SetupGet(x => x.Player).Returns(Player(_senderId));
        playerLogic.SetupGet(x => x.State).Returns(Mock.Of<IPlayerState>(s => s.CurrentRoomId == _roomId));

        h.RoomUser.SetupGet(x => x.Player).Returns(playerLogic.Object);
        h.RoomUser.Setup(x => x.HasRights()).Returns(hasRights);
        h.RoomUser.SetupGet(x => x.NetworkObject).Returns(h.SenderSocket.Object);

        h.SenderSocket
            .Setup(x => x.WriteToStreamAsync(It.IsAny<AbstractPacketWriter>()))
            .Returns((AbstractPacketWriter w) =>
            {
                h.ToSender.Add(w);
                return Task.CompletedTask;
            });

        var users = new Mock<IRoomUserRepository>();
        users.Setup(x => x.GetAll()).Returns([h.RoomUser.Object, .. bystanders]);

        h.Room.SetupGet(x => x.Room).Returns(h.RoomDto);
        h.Room.SetupGet(x => x.UserRepository).Returns(users.Object);
        h.Room
            .Setup(x => x.BroadcastDataAsync(It.IsAny<AbstractPacketWriter>(), It.IsAny<IReadOnlyCollection<long>?>()))
            .Returns((AbstractPacketWriter w, IReadOnlyCollection<long>? excluded) =>
            {
                h.Broadcasts.Add(w);
                h.BroadcastExclusions.Add(excluded);
                return Task.CompletedTask;
            });

        h.Client.SetupGet(x => x.Player).Returns(playerLogic.Object);
        h.Client.SetupGet(x => x.RoomUser).Returns(h.RoomUser.Object);

        h.Rooms.Setup(x => x.TryGetRoomById(_roomId)).Returns(h.Room.Object);

        h.Flood.Setup(x => x.IsMuted(It.IsAny<long>(), out It.Ref<int>.IsAny)).Returns(false);
        h.Flood.Setup(x => x.RegisterMessage(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<bool>())).Returns((int?) null);

        h.RoomWordFilter
            .Setup(x => x.ContainsFilteredWordAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        h.WordFilter
            .Setup(x => x.Filter(It.IsAny<string>(), It.IsAny<WordFilterContext>()))
            .Returns((string text, WordFilterContext _) => new WordFilterResultDto
            {
                OriginalText = text,
                FilteredText = text
            });

        h.RoomHelper
            .Setup(x => x.GetEmotionFromMessage(It.IsAny<string>()))
            .Returns(RoomUserEmotion.None);

        h.Wired
            .Setup(x => x.GetTriggers(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<Ada.API.DTOs.Players.Furniture.PlayerFurnitureItemPlacementDataDto>>(),
                It.IsAny<string>(),
                It.IsAny<List<int>?>()))
            .Returns([]);

        return h;
    }

    [TestCase("")]
    [TestCase("   ")]
    public async Task BlankMessage_IsDropped(string message)
    {
        var h = Build();

        await h.RunAsync(message);

        Assert.That(h.Broadcasts, Is.Empty);
    }

    [Test]
    public async Task MessageOverTheLengthLimit_IsDropped()
    {
        var h = Build();

        await h.RunAsync(new string('a', h.Constants.MaxChatMessageLength + 1));

        Assert.That(h.Broadcasts, Is.Empty, "the length cap must be enforced before anything else runs");
    }

    [Test]
    public async Task MessageAtTheLengthLimit_IsSent()
    {
        var h = Build();

        await h.RunAsync(new string('a', h.Constants.MaxChatMessageLength));

        Assert.That(h.Broadcasts, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task MutedRoom_SilencesUsersWithoutRights()
    {
        var h = Build();
        h.RoomDto.IsMuted = true;

        await h.RunAsync("hello");

        Assert.That(h.Broadcasts, Is.Empty);
    }

    [Test]
    public async Task MutedRoom_StillLetsRightsHoldersSpeak()
    {
        var h = Build(hasRights: true);
        h.RoomDto.IsMuted = true;

        await h.RunAsync("hello");

        Assert.That(h.Broadcasts, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task FloodMutedPlayer_GetsTheRemainingTimeAndNoBroadcast()
    {
        var h = Build();
        var remaining = 12;
        h.Flood.Setup(x => x.IsMuted(_senderId, out remaining)).Returns(true);

        await h.RunAsync("hello");

        Assert.Multiple(() =>
        {
            Assert.That(h.Broadcasts, Is.Empty);
            Assert.That(h.ToSender.OfType<RoomUserFloodControlWriter>().Single().Seconds, Is.EqualTo(12));
        });
    }

    [Test]
    public async Task MessageThatTripsFloodProtection_IsNotBroadcast()
    {
        var h = Build();
        h.Flood.Setup(x => x.RegisterMessage(_senderId, It.IsAny<int>(), It.IsAny<bool>())).Returns(30);

        await h.RunAsync("hello");

        Assert.Multiple(() =>
        {
            Assert.That(h.Broadcasts, Is.Empty);
            Assert.That(h.ToSender.OfType<RoomUserFloodControlWriter>().Single().Seconds, Is.EqualTo(30));
        });
    }

    [Test]
    public async Task RoomOwner_BypassesFloodProtection()
    {
        var h = Build();
        h.RoomDto.OwnerId = _senderId;

        await h.RunAsync("hello");

        h.Flood.Verify(x => x.RegisterMessage(_senderId, It.IsAny<int>(), true), Times.Once);
    }

    [Test]
    public async Task RoomWordFilterMatch_DropsTheMessage()
    {
        var h = Build();
        h.RoomWordFilter.Setup(x => x.ContainsFilteredWordAsync(_roomId, "banned")).ReturnsAsync(true);

        await h.RunAsync("banned");

        Assert.That(h.Broadcasts, Is.Empty);
    }

    [Test]
    public async Task BlockedByTheGlobalFilter_DropsTheMessage()
    {
        var h = Build();
        h.WordFilter
            .Setup(x => x.Filter("bad", WordFilterContext.Chat))
            .Returns(new WordFilterResultDto
            {
                OriginalText = "bad",
                FilteredText = "bad",
                Action = WordFilterAction.Block
            });

        await h.RunAsync("bad");

        Assert.That(h.Broadcasts, Is.Empty);
    }

    [Test]
    public async Task ShadowBlocked_EchoesToTheSenderOnly()
    {
        var h = Build(false, Bystander(99));
        h.WordFilter
            .Setup(x => x.Filter("shady", WordFilterContext.Chat))
            .Returns(new WordFilterResultDto
            {
                OriginalText = "shady",
                FilteredText = "shady",
                Action = WordFilterAction.ShadowBlock
            });

        await h.RunAsync("shady");

        Assert.Multiple(() =>
        {
            Assert.That(h.Broadcasts, Is.Empty, "nobody else may see a shadow-blocked message");
            Assert.That(h.ToSender.OfType<RoomUserChatWriter>().Single().Message, Is.EqualTo("shady"));
        });
    }

    [Test]
    public async Task FilteredText_IsWhatGetsBroadcast()
    {
        var h = Build();
        h.WordFilter
            .Setup(x => x.Filter("rude word", WordFilterContext.Chat))
            .Returns(new WordFilterResultDto
            {
                OriginalText = "rude word",
                FilteredText = "**** word"
            });

        await h.RunAsync("rude word");

        Assert.That(h.Broadcasts.OfType<RoomUserChatWriter>().Single().Message, Is.EqualTo("**** word"),
            "the original text must never reach the room");
    }

    [Test]
    public async Task PlayersIgnoringTheSender_AreExcludedFromTheBroadcast()
    {
        var h = Build(false, Bystander(99, _senderId), Bystander(100));

        await h.RunAsync("hello");

        Assert.That(h.BroadcastExclusions.Single(), Is.EqualTo(new[] { 99L }));
    }

    [Test]
    public async Task Shouting_UsesTheShoutWriterAndSkipsCommands()
    {
        var h = Build();

        await h.RunAsync(":kick", shouting: true);

        Assert.Multiple(() =>
        {
            Assert.That(h.Broadcasts.Single(), Is.TypeOf<RoomUserShoutWriter>());
            Assert.That(h.RoomDto.ChatMessages.Single().TypeId, Is.EqualTo(RoomChatMessageType.Shout));
        });
    }

    [Test]
    public async Task SuccessfulMessage_IsRecordedInTheRoomLog()
    {
        var h = Build();

        await h.RunAsync("hello there");

        var logged = h.RoomDto.ChatMessages.Single();

        Assert.Multiple(() =>
        {
            Assert.That(logged.Message, Is.EqualTo("hello there"));
            Assert.That(logged.PlayerId, Is.EqualTo(_senderId));
            Assert.That(logged.RoomId, Is.EqualTo(_roomId));
            Assert.That(logged.TypeId, Is.EqualTo(RoomChatMessageType.Normal));
        });
    }

    [Test]
    public async Task ClientWithNoRoomUser_IsIgnored()
    {
        var h = Build();
        h.Client.SetupGet(x => x.RoomUser).Returns((IRoomUser?) null);

        await h.RunAsync("hello");

        Assert.That(h.Broadcasts, Is.Empty);
    }
}
