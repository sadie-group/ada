using Ada.API.Interfaces.Game.Players;
using Ada.Game.Rooms.Services;
using Ada.Tests.Common;
using Moq;

namespace Ada.Tests.Game.Rooms.Services;

[TestFixture]
public class FloodProtectionSessionListenerTests
{
    private static IPlayerLogic Player(long id)
    {
        var player = new Mock<IPlayerLogic>();
        player.SetupGet(x => x.Player).Returns(TestPlayers.Minimal(id, $"p{id}"));

        return player.Object;
    }

    [Test]
    public async Task Disconnect_DropsTheFloodStateForThatPlayer()
    {
        var service = new RoomFloodProtectionService();
        var listener = new FloodProtectionSessionListener(service);

        service.MuteFor(7, seconds: 600);
        Assert.That(service.IsMuted(7, out _), Is.True);

        await listener.OnDisconnectedAsync(Player(7), null);

        Assert.That(service.IsMuted(7, out _), Is.False,
            "state must not survive the session that created it");
    }

    [Test]
    public async Task Disconnect_LeavesOtherPlayersAlone()
    {
        var service = new RoomFloodProtectionService();
        var listener = new FloodProtectionSessionListener(service);

        service.MuteFor(1, seconds: 600);
        service.MuteFor(2, seconds: 600);

        await listener.OnDisconnectedAsync(Player(1), null);

        Assert.Multiple(() =>
        {
            Assert.That(service.IsMuted(1, out _), Is.False);
            Assert.That(service.IsMuted(2, out _), Is.True);
        });
    }
}

[TestFixture]
public class RoomFloodProtectionServiceEscalationTests
{
    private const int Threshold = 3;

    private static int? FloodUntilMuted(RoomFloodProtectionService service, long playerId)
    {
        for (var i = 0; i <= Threshold + 1; i++)
        {
            var mute = service.RegisterMessage(playerId, chatProtection: 0, bypass: false);

            if (mute != null)
            {
                return mute;
            }
        }

        return null;
    }

    [Test]
    public void Escalation_IsCappedRatherThanGrowingWithoutBound()
    {
        var service = new RoomFloodProtectionService();
        var last = 0;

        for (var round = 0; round < 40; round++)
        {
            var mute = FloodUntilMuted(service, 1);

            Assert.That(mute, Is.Not.Null, "the burst must trip flood control every round");
            last = mute!.Value;
        }

        Assert.That(last, Is.EqualTo(3030),
            "an uncapped quadratic curve reached day-long mutes and eventually overflowed");
    }

    [Test]
    public void Escalation_StaysPositive()
    {
        var service = new RoomFloodProtectionService();

        for (var round = 0; round < 40; round++)
        {
            var mute = FloodUntilMuted(service, 2);

            Assert.That(mute, Is.Not.Null.And.GreaterThan(0),
                "a negative mute would expire instantly and disable flood control entirely");
        }
    }
}
