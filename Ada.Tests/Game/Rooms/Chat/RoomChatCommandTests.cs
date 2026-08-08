using Ada.API;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Locale;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Chat.Commands;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Networking;
using Ada.Game.Rooms.Chat.Commands;
using Ada.Networking.Writers.Players;
using Moq;

namespace Ada.Tests.Game.Rooms.Chat;

public static class ChatCommandTestHelpers
{
    public static (IRoomUser User, List<AbstractPacketWriter> Written) CreateUser()
    {
        var written = new List<AbstractPacketWriter>();
        var networkObject = new Mock<INetworkObject>();
        networkObject
            .Setup(x => x.WriteToStreamAsync(It.IsAny<AbstractPacketWriter>()))
            .Callback<AbstractPacketWriter>(written.Add)
            .Returns(Task.CompletedTask);

        var user = new Mock<IRoomUser>();
        user.Setup(x => x.NetworkObject).Returns(networkObject.Object);

        return (user.Object, written);
    }

    public static ILocaleService CreateLocale(string key, string value)
    {
        var locale = new Mock<ILocaleService>();
        locale.Setup(x => x[key]).Returns(value);
        return locale.Object;
    }
}

[TestFixture]
public class AboutCommandTests
{
    private static AboutCommand CreateCommand(long playersOnline = 0, int roomCount = 0)
    {
        var roomRepository = new Mock<IRoomRepository>();
        roomRepository.Setup(x => x.Count).Returns(roomCount);

        var playerRepository = new Mock<IPlayerRepository>();
        playerRepository.Setup(x => x.Count()).Returns(playersOnline);

        return new AboutCommand(
            roomRepository.Object,
            playerRepository.Object,
            ChatCommandTestHelpers.CreateLocale("cmd.about.describe", "about the server"));
    }

    [Test]
    public void Trigger_IsAbout()
    {
        Assert.That(CreateCommand().Trigger, Is.EqualTo("about"));
    }

    [Test]
    public void Description_ReadsLocale()
    {
        Assert.That(CreateCommand().Description, Is.EqualTo("about the server"));
    }

    [Test]
    public void Defaults_NoPermissionsNoParameters()
    {
        var command = CreateCommand();
        Assert.Multiple(() =>
        {
            Assert.That(command.PermissionsRequired, Is.Empty);
            Assert.That(command.BypassPermissionCheckIfRoomOwner, Is.False);
            Assert.That(command.Parameters, Is.Empty);
        });
    }

    [Test]
    public async Task ExecuteAsync_WritesServerStatsAlert()
    {
        var command = CreateCommand(playersOnline: 7, roomCount: 3);
        var (user, written) = ChatCommandTestHelpers.CreateUser();

        await command.ExecuteAsync(user, Mock.Of<IRoomChatCommandParameterReader>());

        Assert.That(written, Has.Count.EqualTo(1));
        var alert = (PlayerAlertWriter)written[0];
        Assert.That(alert.Message, Does.StartWith("Ada"));
        Assert.That(alert.Message, Does.Contain("Players Online: 7"));
        Assert.That(alert.Message, Does.Contain("Rooms Loaded: 3"));
        Assert.That(alert.Message, Does.Contain("Memory Used:"));
        Assert.That(alert.Message, Does.Contain("MB"));
        Assert.That(alert.Message, Does.Contain("Credits:"));
        Assert.That(alert.Message, Does.Contain("Habtard - Lead Developer"));
        Assert.That(alert.Message, Does.Contain("Damien - Encryption"));
    }
}

[TestFixture]
public class RoomsCommandTests
{
    private static Mock<IRoomLogic> CreateRoom(int id, int userCount, DateTime? noUsersSince = null)
    {
        var room = new Mock<IRoomLogic>();
        room.Setup(x => x.Room).Returns(new RoomDto { Id = id });
        room.Setup(x => x.UserRepository.Count).Returns(userCount);
        room.Setup(x => x.UserRepository.NoUsersSince).Returns(noUsersSince);
        return room;
    }

    private static RoomsCommand CreateCommand(params Mock<IRoomLogic>[] rooms)
    {
        var roomRepository = new Mock<IRoomRepository>();
        roomRepository.Setup(x => x.GetAllRooms()).Returns(rooms.Select(r => r.Object));

        return new RoomsCommand(
            roomRepository.Object,
            ChatCommandTestHelpers.CreateLocale("cmd.rooms.describe", "list rooms"));
    }

    [Test]
    public void Trigger_IsRooms()
    {
        Assert.That(CreateCommand().Trigger, Is.EqualTo("rooms"));
    }

    [Test]
    public void Description_ReadsLocale()
    {
        Assert.That(CreateCommand().Description, Is.EqualTo("list rooms"));
    }

    [Test]
    public async Task ExecuteAsync_ListsRoomsByUserCountDescending()
    {
        var idle = new DateTime(2026, 1, 1, 12, 0, 0);
        var command = CreateCommand(CreateRoom(1, 1, idle), CreateRoom(2, 5));
        var (user, written) = ChatCommandTestHelpers.CreateUser();

        await command.ExecuteAsync(user, Mock.Of<IRoomChatCommandParameterReader>());

        Assert.That(written, Has.Count.EqualTo(1));
        var alert = (PlayerAlertWriter)written[0];
        var expected = string.Join(Environment.NewLine, "2: 5 users, ", $"1: 1 users, {idle}");
        Assert.That(alert.Message, Is.EqualTo(expected));
    }

    [Test]
    public async Task ExecuteAsync_NoRooms_WritesEmptyAlert()
    {
        var command = CreateCommand();
        var (user, written) = ChatCommandTestHelpers.CreateUser();

        await command.ExecuteAsync(user, Mock.Of<IRoomChatCommandParameterReader>());

        Assert.That(((PlayerAlertWriter)written.Single()).Message, Is.Empty);
    }
}

[TestFixture]
public class RoomChatCommandRepositoryTests
{
    private static IRoomChatCommand CreateChatCommand(string trigger)
    {
        var command = new Mock<IRoomChatCommand>();
        command.Setup(x => x.Trigger).Returns(trigger);
        return command.Object;
    }

    [Test]
    public void TryGetCommandByTriggerWord_Known_ReturnsCommand()
    {
        var about = CreateChatCommand("about");
        var repository = new RoomChatCommandRepository([about, CreateChatCommand("rooms")]);

        Assert.That(repository.TryGetCommandByTriggerWord("about"), Is.SameAs(about));
    }

    [Test]
    public void TryGetCommandByTriggerWord_Unknown_ReturnsNull()
    {
        var repository = new RoomChatCommandRepository([CreateChatCommand("about")]);

        Assert.That(repository.TryGetCommandByTriggerWord("missing"), Is.Null);
    }

    [Test]
    public void GetRegisteredCommands_ReturnsAll()
    {
        var repository = new RoomChatCommandRepository(
            [CreateChatCommand("about"), CreateChatCommand("rooms")]);

        Assert.That(repository.GetRegisteredCommands(), Has.Count.EqualTo(2));
    }
}
