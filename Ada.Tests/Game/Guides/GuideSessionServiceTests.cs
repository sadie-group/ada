using Ada.Tests.Common;
using Ada.API.Interfaces.Game.Guides;
using Ada.API.Interfaces.Game.Players;
using Ada.Core.Enums.Game.Players;
using Ada.Game.Guides;
using Moq;

namespace Ada.Tests.Game.Guides;

[TestFixture]
public class GuideSessionServiceTests
{
    private static IPlayerLogic Helper(long id, bool hasPermission = true)
    {
        var player = new Mock<IPlayerLogic>();
        player.SetupGet(p => p.Player).Returns(TestPlayers.Minimal(id, $"helper{id}"));
        player.Setup(p => p.HasPermission(PlayerPermissionName.GuideUseTool)).Returns(hasPermission);
        return player.Object;
    }

    [Test]
    public void SetOnDuty_TracksGuidesAndGuardiansSeparately()
    {
        var service = new GuideSessionService();

        service.SetOnDuty(1, true, false);
        service.SetOnDuty(2, true, true);

        Assert.Multiple(() =>
        {
            Assert.That(service.GuidesOnDuty, Is.EqualTo(1));
            Assert.That(service.GuardiansOnDuty, Is.EqualTo(1));
            Assert.That(service.IsOnDuty(1), Is.True);
            Assert.That(service.IsOnDuty(2), Is.False);
        });
    }

    [Test]
    public void TryAssignHelper_NoHelpersOnDuty_ReturnsNull()
    {
        var service = new GuideSessionService();
        var session = service.CreateSession(requesterId: 10, requestType: 1, helpRequest: "help");

        Assert.That(service.TryAssignHelper(session, [Helper(1)]), Is.Null);
    }

    [Test]
    public void TryAssignHelper_HelperOnDuty_Assigns()
    {
        var service = new GuideSessionService();
        service.SetOnDuty(1, true, false);

        var session = service.CreateSession(10, 1, "help");

        Assert.Multiple(() =>
        {
            Assert.That(service.TryAssignHelper(session, [Helper(1)]), Is.EqualTo(1));
            Assert.That(service.GetPendingSessionForHelper(1), Is.Not.Null);
        });
    }

    [Test]
    public void TryAssignHelper_RequesterIsOnDuty_IsNotAssignedToSelf()
    {
        var service = new GuideSessionService();
        service.SetOnDuty(10, true, false);

        var session = service.CreateSession(10, 1, "help");

        Assert.That(service.TryAssignHelper(session, [Helper(10)]), Is.Null);
    }

    [Test]
    public void TryAssignHelper_WithoutPermission_IsSkipped()
    {
        var service = new GuideSessionService();
        service.SetOnDuty(1, true, false);

        var session = service.CreateSession(10, 1, "help");

        Assert.That(service.TryAssignHelper(session, [Helper(1, hasPermission: false)]), Is.Null);
    }

    [Test]
    public void TryAssignHelper_AlreadyAssigned_DoesNotReassign()
    {
        var service = new GuideSessionService();
        service.SetOnDuty(1, true, false);
        service.SetOnDuty(2, true, false);

        var session = service.CreateSession(10, 1, "help");
        service.TryAssignHelper(session, [Helper(1)]);

        Assert.That(service.TryAssignHelper(session, [Helper(2)]), Is.Null);
    }

    [Test]
    public void Decline_ReleasesHelperAndDoesNotOfferAgain()
    {
        var service = new GuideSessionService();
        service.SetOnDuty(1, true, false);
        service.SetOnDuty(2, true, false);

        var session = service.CreateSession(10, 1, "help");
        service.TryAssignHelper(session, [Helper(1)]);
        service.Decline(session);

        Assert.Multiple(() =>
        {
            Assert.That(service.GetPendingSessionForHelper(1), Is.Null);
            Assert.That(service.TryAssignHelper(session, [Helper(1), Helper(2)]), Is.EqualTo(2));
        });
    }

    [Test]
    public void Accept_MovesSessionToActive()
    {
        var service = new GuideSessionService();
        service.SetOnDuty(1, true, false);

        var session = service.CreateSession(10, 1, "help");
        service.TryAssignHelper(session, [Helper(1)]);

        Assert.Multiple(() =>
        {
            Assert.That(service.Accept(session, 1), Is.True);
            Assert.That(session.State, Is.EqualTo(GuideSessionState.Active));
            Assert.That(service.GetPendingSessionForHelper(1), Is.Null);
        });
    }

    [Test]
    public void Accept_ByAnotherHelper_IsRejected()
    {
        var service = new GuideSessionService();
        service.SetOnDuty(1, true, false);

        var session = service.CreateSession(10, 1, "help");
        service.TryAssignHelper(session, [Helper(1)]);

        Assert.That(service.Accept(session, 99), Is.False);
    }

    [Test]
    public void GetSessionForPlayer_ResolvesFromEitherSide()
    {
        var service = new GuideSessionService();
        service.SetOnDuty(1, true, false);

        var session = service.CreateSession(10, 1, "help");
        service.TryAssignHelper(session, [Helper(1)]);

        Assert.Multiple(() =>
        {
            Assert.That(service.GetSessionForPlayer(10), Is.SameAs(session));
            Assert.That(service.GetSessionForPlayer(1), Is.SameAs(session));
        });
    }

    [Test]
    public void End_ClearsBothSides()
    {
        var service = new GuideSessionService();
        service.SetOnDuty(1, true, false);

        var session = service.CreateSession(10, 1, "help");
        service.TryAssignHelper(session, [Helper(1)]);
        service.Accept(session, 1);

        Assert.That(service.End(session), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(service.GetSessionForPlayer(10), Is.Null);
            Assert.That(service.GetSessionForPlayer(1), Is.Null);
        });
    }

    [Test]
    public void Forget_RemovesDutyAndEndsSession()
    {
        var service = new GuideSessionService();
        service.SetOnDuty(1, true, false);

        var session = service.CreateSession(10, 1, "help");
        service.TryAssignHelper(session, [Helper(1)]);
        service.Accept(session, 1);

        service.Forget(1);

        Assert.Multiple(() =>
        {
            Assert.That(service.IsOnDuty(1), Is.False);
            Assert.That(service.GetSessionForPlayer(10), Is.Null);
            Assert.That(service.GuidesOnDuty, Is.EqualTo(0));
        });
    }

    [Test]
    public void AverageWaitSeconds_DefaultsBeforeAnySessionCompletes()
    {
        Assert.That(new GuideSessionService().AverageWaitSeconds, Is.EqualTo(60));
    }

    [Test]
    public void AverageWaitSeconds_UsesObservedWaits()
    {
        var service = new GuideSessionService();
        service.SetOnDuty(1, true, false);

        var session = service.CreateSession(10, 1, "help");
        service.TryAssignHelper(session, [Helper(1)]);
        service.Accept(session, 1);

        Assert.That(service.AverageWaitSeconds, Is.LessThan(60));
    }

    [Test]
    public void HelperInASession_IsNotOfferedAnother()
    {
        var service = new GuideSessionService();
        service.SetOnDuty(1, true, false);

        var first = service.CreateSession(10, 1, "help");
        service.TryAssignHelper(first, [Helper(1)]);
        service.Accept(first, 1);

        var second = service.CreateSession(11, 1, "help");

        Assert.That(service.TryAssignHelper(second, [Helper(1)]), Is.Null);
    }
}
