using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms.Chat.Commands;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Rooms;
using Ada.Game.Rooms.Chat.Commands;
using Moq;

namespace Ada.Tests.Game.Rooms.Chat;

[TestFixture]
public class RoomCommandServiceTests
{
    private const string Trigger = "kick";
    private const string Permission = "command_kick";

    private static (IRoomChatCommandRepository Repository, Mock<IRoomChatCommand> Command) Repository(
        bool bypassForOwner)
    {
        var command = new Mock<IRoomChatCommand>();
        command.SetupGet(x => x.Trigger).Returns(Trigger);
        command.SetupGet(x => x.PermissionsRequired).Returns([Permission]);
        command.SetupGet(x => x.BypassPermissionCheckIfRoomOwner).Returns(bypassForOwner);
        command.Setup(x => x.ExecuteAsync(It.IsAny<IRoomUser>(), It.IsAny<IRoomChatCommandParameterReader>()))
            .Returns(Task.CompletedTask);

        var repository = new Mock<IRoomChatCommandRepository>();
        repository.Setup(x => x.TryGetCommandByTriggerWord(Trigger)).Returns(command.Object);

        return (repository.Object, command);
    }

    private static IRoomUser User(RoomControllerLevel level, bool hasPermission)
    {
        var player = new Mock<IPlayerLogic>();
        player.Setup(x => x.HasPermission(Permission)).Returns(hasPermission);

        var user = new Mock<IRoomUser>();
        user.SetupGet(x => x.ControllerLevel).Returns(level);
        user.SetupGet(x => x.Player).Returns(player.Object);

        return user.Object;
    }

    [Test]
    public async Task OwnerBypass_OwnerWithoutPermission_RunsTheCommand()
    {
        var (repository, command) = Repository(bypassForOwner: true);

        var executed = await RoomCommandService.TryExecuteAsync(
            repository, $":{Trigger}", User(RoomControllerLevel.Owner, hasPermission: false));

        Assert.That(executed, Is.True, "the owner bypass is the whole point of the flag");
        command.Verify(x => x.ExecuteAsync(It.IsAny<IRoomUser>(), It.IsAny<IRoomChatCommandParameterReader>()),
            Times.Once);
    }

    [Test]
    public async Task OwnerBypass_NonOwnerWithPermission_StillRunsTheCommand()
    {
        var (repository, command) = Repository(bypassForOwner: true);

        var executed = await RoomCommandService.TryExecuteAsync(
            repository, $":{Trigger}", User(RoomControllerLevel.Rights, hasPermission: true));

        Assert.That(executed, Is.True, "the flag widens access for owners, it does not restrict others");
        command.Verify(x => x.ExecuteAsync(It.IsAny<IRoomUser>(), It.IsAny<IRoomChatCommandParameterReader>()),
            Times.Once);
    }

    [Test]
    public async Task OwnerBypass_NonOwnerWithoutPermission_IsRefused()
    {
        var (repository, command) = Repository(bypassForOwner: true);

        var executed = await RoomCommandService.TryExecuteAsync(
            repository, $":{Trigger}", User(RoomControllerLevel.None, hasPermission: false));

        Assert.That(executed, Is.False);
        command.Verify(x => x.ExecuteAsync(It.IsAny<IRoomUser>(), It.IsAny<IRoomChatCommandParameterReader>()),
            Times.Never);
    }

    [Test]
    public async Task NoOwnerBypass_OwnerWithoutPermission_IsRefused()
    {
        var (repository, command) = Repository(bypassForOwner: false);

        var executed = await RoomCommandService.TryExecuteAsync(
            repository, $":{Trigger}", User(RoomControllerLevel.Owner, hasPermission: false));

        Assert.That(executed, Is.False, "without the flag, being the owner grants nothing");
        command.Verify(x => x.ExecuteAsync(It.IsAny<IRoomUser>(), It.IsAny<IRoomChatCommandParameterReader>()),
            Times.Never);
    }
}
