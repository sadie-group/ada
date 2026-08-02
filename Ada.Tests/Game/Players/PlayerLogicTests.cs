using Ada.API;
using Ada.API.DTOs;
using Ada.API.DTOs.Players;
using Ada.API.DTOs.Server;
using Ada.API.Interfaces.Networking;
using Ada.Core.Enums.Game.Players;
using Ada.Game.Players;
using Ada.Networking.Writers.Players;
using Microsoft.Extensions.Logging;
using Moq;

namespace Ada.Tests.Game.Players;

[TestFixture]
public class PlayerLogicTests
{
    private static PlayerDto CreateDto(
        ICollection<PlayerFriendshipDto>? outgoing = null,
        ICollection<PlayerFriendshipDto>? incoming = null,
        List<RoleDto>? roles = null,
        ICollection<ServerPeriodicCurrencyRewardLogDto>? rewardLogs = null) => new(
        1, "alice", "mail@test.com", DateTimeOffset.UnixEpoch,
        roles ?? [], null, null, [], [], [], [], null, null, [], [], [], [], [], [],
        outgoing ?? [], incoming ?? [], [], [], rewardLogs ?? [], [], [], [], [], [], []);

    private static PlayerFriendshipDto Friendship(long origin, long target, PlayerFriendshipStatus status) =>
        new() { OriginPlayerId = origin, TargetPlayerId = target, Status = status };

    private static PlayerLogic CreateLogic(PlayerDto dto) =>
        new(Mock.Of<ILogger<PlayerLogic>>(), dto);

    [Test]
    public void Properties_ExposeConstructionState()
    {
        var dto = CreateDto();
        var logic = CreateLogic(dto);
        logic.Authenticated = true;

        Assert.Multiple(() =>
        {
            Assert.That(logic.Player, Is.SameAs(dto));
            Assert.That(logic.State, Is.Not.Null);
            Assert.That(logic.Authenticated, Is.True);
            Assert.That(logic.NetworkObject, Is.Null);
        });
    }

    [Test]
    public void GetAcceptedFriendshipCount_CountsAcceptedInBothDirections()
    {
        var logic = CreateLogic(CreateDto(
            outgoing:
            [
                Friendship(1, 2, PlayerFriendshipStatus.Accepted),
                Friendship(1, 3, PlayerFriendshipStatus.Accepted),
                Friendship(1, 4, PlayerFriendshipStatus.Pending)
            ],
            incoming:
            [
                Friendship(5, 1, PlayerFriendshipStatus.Accepted),
                Friendship(6, 1, PlayerFriendshipStatus.Pending)
            ]));

        Assert.That(logic.GetAcceptedFriendshipCount(), Is.EqualTo(3));
    }

    [Test]
    public void GetMergedFriendships_ReturnsAcceptedFromBothDirections()
    {
        var outgoingAccepted = Friendship(1, 2, PlayerFriendshipStatus.Accepted);
        var incomingAccepted = Friendship(5, 1, PlayerFriendshipStatus.Accepted);
        var logic = CreateLogic(CreateDto(
            outgoing: [outgoingAccepted, Friendship(1, 4, PlayerFriendshipStatus.Pending)],
            incoming: [incomingAccepted, Friendship(6, 1, PlayerFriendshipStatus.Pending)]));

        Assert.That(logic.GetMergedFriendships(), Is.EquivalentTo(new[] { outgoingAccepted, incomingAccepted }));
    }

    [Test]
    public void IsFriendsWith_AcceptedIncoming_ReturnsTrue()
    {
        var logic = CreateLogic(CreateDto(incoming: [Friendship(5, 1, PlayerFriendshipStatus.Accepted)]));

        Assert.That(logic.IsFriendsWith(5), Is.True);
    }

    [Test]
    public void IsFriendsWith_AcceptedOutgoing_ReturnsTrue()
    {
        var logic = CreateLogic(CreateDto(outgoing: [Friendship(1, 7, PlayerFriendshipStatus.Accepted)]));

        Assert.That(logic.IsFriendsWith(7), Is.True);
    }

    [Test]
    public void IsFriendsWith_PendingOrUnknown_ReturnsFalse()
    {
        var logic = CreateLogic(CreateDto(
            outgoing: [Friendship(1, 7, PlayerFriendshipStatus.Pending)],
            incoming: [Friendship(5, 1, PlayerFriendshipStatus.Pending)]));

        Assert.Multiple(() =>
        {
            Assert.That(logic.IsFriendsWith(5), Is.False);
            Assert.That(logic.IsFriendsWith(7), Is.False);
            Assert.That(logic.IsFriendsWith(99), Is.False);
        });
    }

    [Test]
    public void TryGetAcceptedFriendshipFor_IncomingAccepted_ReturnsIt()
    {
        var incoming = Friendship(5, 1, PlayerFriendshipStatus.Accepted);
        var logic = CreateLogic(CreateDto(incoming: [incoming]));

        Assert.That(logic.TryGetAcceptedFriendshipFor(5), Is.SameAs(incoming));
    }

    [Test]
    public void TryGetAcceptedFriendshipFor_OutgoingMatchedByOrigin_ReturnsIt()
    {
        var outgoing = Friendship(9, 1, PlayerFriendshipStatus.Accepted);
        var logic = CreateLogic(CreateDto(outgoing: [outgoing]));

        Assert.That(logic.TryGetAcceptedFriendshipFor(9), Is.SameAs(outgoing));
    }

    [Test]
    public void TryGetAcceptedFriendshipFor_NoAcceptedMatch_ReturnsNull()
    {
        var logic = CreateLogic(CreateDto(
            outgoing: [Friendship(1, 9, PlayerFriendshipStatus.Accepted)],
            incoming: [Friendship(9, 1, PlayerFriendshipStatus.Pending)]));

        Assert.That(logic.TryGetAcceptedFriendshipFor(9), Is.Null);
    }

    [Test]
    public void TryGetFriendshipFor_Incoming_ReturnsAnyStatus()
    {
        var incoming = Friendship(5, 1, PlayerFriendshipStatus.Pending);
        var logic = CreateLogic(CreateDto(incoming: [incoming]));

        Assert.That(logic.TryGetFriendshipFor(5), Is.SameAs(incoming));
    }

    [Test]
    public void TryGetFriendshipFor_OutgoingMatchedByTarget_ReturnsIt()
    {
        var outgoing = Friendship(1, 7, PlayerFriendshipStatus.Pending);
        var logic = CreateLogic(CreateDto(outgoing: [outgoing]));

        Assert.That(logic.TryGetFriendshipFor(7), Is.SameAs(outgoing));
    }

    [Test]
    public void TryGetFriendshipFor_Unknown_ReturnsNull()
    {
        var logic = CreateLogic(CreateDto());

        Assert.That(logic.TryGetFriendshipFor(42), Is.Null);
    }

    [Test]
    public void DeleteFriendshipFor_RemovesMatchesByOriginFromBothLists()
    {
        var incomingMatch = Friendship(5, 1, PlayerFriendshipStatus.Accepted);
        var incomingOther = Friendship(6, 1, PlayerFriendshipStatus.Accepted);
        var outgoingMatch = Friendship(5, 9, PlayerFriendshipStatus.Accepted);
        var outgoingOther = Friendship(1, 5, PlayerFriendshipStatus.Accepted);
        var dto = CreateDto(
            outgoing: [outgoingMatch, outgoingOther],
            incoming: [incomingMatch, incomingOther]);
        var logic = CreateLogic(dto);

        logic.DeleteFriendshipFor(5);

        Assert.Multiple(() =>
        {
            Assert.That(dto.IncomingFriendships, Is.EquivalentTo(new[] { incomingOther }));
            Assert.That(dto.OutgoingFriendships, Is.EquivalentTo(new[] { outgoingOther }));
        });
    }

    [Test]
    public void DeleteFriendshipFor_NoMatch_LeavesListsUntouched()
    {
        var incoming = Friendship(6, 1, PlayerFriendshipStatus.Accepted);
        var dto = CreateDto(incoming: [incoming]);
        var logic = CreateLogic(dto);

        logic.DeleteFriendshipFor(42);

        Assert.That(dto.IncomingFriendships, Is.EquivalentTo(new[] { incoming }));
    }

    [Test]
    public void HasPermission_MatchingPermission_ReturnsTrue()
    {
        var logic = CreateLogic(CreateDto(roles:
        [
            new RoleDto { Id = 1, Name = "user" },
            new RoleDto { Id = 2, Name = "mod", Permissions = [new PermissionDto { Id = 1, Name = "kick" }] }
        ]));

        Assert.Multiple(() =>
        {
            Assert.That(logic.HasPermission("kick"), Is.True);
            Assert.That(logic.HasPermission("ban"), Is.False);
        });
    }

    [Test]
    public void DeservesReward_NoPriorLog_ReturnsTrue()
    {
        var logic = CreateLogic(CreateDto());

        Assert.That(logic.DeservesReward("daily", 3600), Is.True);
    }

    [Test]
    public void DeservesReward_RecentLog_ReturnsFalse()
    {
        var logic = CreateLogic(CreateDto(rewardLogs:
        [
            new ServerPeriodicCurrencyRewardLogDto { Id = 1, Type = "daily", CreatedAt = DateTimeOffset.Now }
        ]));

        Assert.That(logic.DeservesReward("daily", 3600), Is.False);
    }

    [Test]
    public void DeservesReward_StaleLog_ReturnsTrue()
    {
        var logic = CreateLogic(CreateDto(rewardLogs:
        [
            new ServerPeriodicCurrencyRewardLogDto { Id = 1, Type = "daily", CreatedAt = DateTimeOffset.Now.AddHours(-2) }
        ]));

        Assert.That(logic.DeservesReward("daily", 3600), Is.True);
    }

    [Test]
    public void DeservesReward_OtherRewardType_ReturnsTrue()
    {
        var logic = CreateLogic(CreateDto(rewardLogs:
        [
            new ServerPeriodicCurrencyRewardLogDto { Id = 1, Type = "daily", CreatedAt = DateTimeOffset.Now }
        ]));

        Assert.That(logic.DeservesReward("weekly", 3600), Is.True);
    }

    [Test]
    public async Task SendAlertAsync_WritesAlertToNetworkObject()
    {
        var network = new Mock<INetworkObject>();
        AbstractPacketWriter? written = null;
        network.Setup(x => x.WriteToStreamAsync(It.IsAny<AbstractPacketWriter>()))
            .Callback((AbstractPacketWriter w) => written = w)
            .Returns(Task.CompletedTask);
        var logic = CreateLogic(CreateDto());
        logic.NetworkObject = network.Object;

        await logic.SendAlertAsync("maintenance soon");

        Assert.That(written, Is.InstanceOf<PlayerAlertWriter>());
        Assert.That(((PlayerAlertWriter)written!).Message, Is.EqualTo("maintenance soon"));
    }

    [Test]
    public async Task DisposeAsync_Completes()
    {
        var logic = CreateLogic(CreateDto());

        await logic.DisposeAsync();
    }
}
