using Ada.API.DTOs.Players;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Rooms;
using Ada.Networking.Events.Handlers.Rooms.Moderation;
using Moq;

namespace Ada.Tests.Networking.Events.Handlers.Rooms.Moderation;

[TestFixture]
public class RoomModerationRulesTests
{
    private static IRoomUser User(long id, RoomControllerLevel level)
    {
        var player = new Mock<IPlayerLogic>();
        player.SetupGet(x => x.Player).Returns(new PlayerDto(
            id, $"user{id}", "t@test.com", DateTimeOffset.UtcNow,
            [], new PlayerDataDto(), new PlayerAvatarDataDto(), [], [], [], [],
            new PlayerNavigatorSettingsDto(), new PlayerGameSettingsDto(),
            [], [], [], [], [], [], [], [], [], [], [], [], [], [], [], [], []));

        var user = new Mock<IRoomUser>();
        user.SetupGet(x => x.Player).Returns(player.Object);
        user.SetupGet(x => x.ControllerLevel).Returns(level);

        return user.Object;
    }

    private static IRoomLogic Room(long ownerId, params IRoomUser[] users)
    {
        var repo = new Mock<IRoomUserRepository>();
        repo.Setup(x => x.GetAll()).Returns(users.ToList());

        var room = new Mock<IRoomLogic>();
        room.SetupGet(x => x.Room).Returns(new RoomDto { Id = 1, OwnerId = ownerId });
        room.SetupGet(x => x.UserRepository).Returns(repo.Object);

        return room.Object;
    }

    [Test]
    public void CanActOn_Self_IsRefused()
    {
        var actor = User(5, RoomControllerLevel.Owner);

        Assert.That(RoomModerationRules.CanActOn(Room(99, actor), actor, 5, out _), Is.False);
    }

    [Test]
    public void CanActOn_RoomOwner_IsRefused()
    {
        var actor = User(5, RoomControllerLevel.Moderator);
        var owner = User(9, RoomControllerLevel.Owner);

        Assert.That(RoomModerationRules.CanActOn(Room(9, actor, owner), actor, 9, out _), Is.False);
    }

    [Test]
    public void CanActOn_TargetNotInRoom_IsRefused()
    {
        var actor = User(5, RoomControllerLevel.Owner);

        Assert.That(RoomModerationRules.CanActOn(Room(99, actor), actor, 1234, out _), Is.False);
    }

    [Test]
    public void CanActOn_TargetOfEqualRank_IsRefused()
    {
        var actor = User(5, RoomControllerLevel.Rights);
        var peer = User(6, RoomControllerLevel.Rights);

        Assert.That(RoomModerationRules.CanActOn(Room(99, actor, peer), actor, 6, out _), Is.False);
    }

    [Test]
    public void CanActOn_TargetOfHigherRank_IsRefused()
    {
        var actor = User(5, RoomControllerLevel.Rights);
        var senior = User(6, RoomControllerLevel.Moderator);

        Assert.That(RoomModerationRules.CanActOn(Room(99, actor, senior), actor, 6, out _), Is.False);
    }

    [Test]
    public void CanActOn_LowerRankedTargetInRoom_IsAllowed()
    {
        var actor = User(5, RoomControllerLevel.Owner);
        var target = User(6, RoomControllerLevel.None);

        var allowed = RoomModerationRules.CanActOn(Room(99, actor, target), actor, 6, out var resolved);

        Assert.Multiple(() =>
        {
            Assert.That(allowed, Is.True);
            Assert.That(resolved, Is.SameAs(target));
        });
    }
}
