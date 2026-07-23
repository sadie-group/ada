using Ada.Game.Rooms.Services;

namespace Ada.Tests.Game.Rooms.Services;

public class RoomFloodProtectionServiceTests
{
    private RoomFloodProtectionService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new RoomFloodProtectionService();
    }

    [Test]
    public void RegisterMessage_UnderThreshold_DoesNotMute()
    {
        for (var i = 0; i < 3; i++)
        {
            Assert.That(_service.RegisterMessage(1, 0, false), Is.Null);
        }

        Assert.That(_service.IsMuted(1, out _), Is.False);
    }

    [Test]
    public void RegisterMessage_OverThreshold_Mutes()
    {
        int? muteSeconds = null;

        for (var i = 0; i < 5 && muteSeconds == null; i++)
        {
            muteSeconds = _service.RegisterMessage(1, 0, false);
        }

        Assert.Multiple(() =>
        {
            Assert.That(muteSeconds, Is.EqualTo(RoomFloodProtectionService.BaseMuteSeconds * 2));
            Assert.That(_service.IsMuted(1, out var remaining), Is.True);
            Assert.That(remaining, Is.GreaterThan(0));
        });
    }

    [Test]
    public void RegisterMessage_RepeatOffender_MuteEscalates()
    {
        int? first = null;

        for (var i = 0; i < 10 && first == null; i++)
        {
            first = _service.RegisterMessage(1, 0, false);
        }

        int? second = null;

        for (var i = 0; i < 10 && second == null; i++)
        {
            second = _service.RegisterMessage(1, 0, false);
        }

        Assert.That(second, Is.GreaterThan(first));
    }

    [Test]
    public void RegisterMessage_LooserChatProtection_AllowsMoreMessages()
    {
        var strictMessages = CountMessagesUntilMute(1, chatProtection: 0);
        var looseMessages = CountMessagesUntilMute(2, chatProtection: 2);

        Assert.That(looseMessages, Is.GreaterThan(strictMessages));
    }

    [Test]
    public void RegisterMessage_Bypass_NeverMutes()
    {
        for (var i = 0; i < 20; i++)
        {
            Assert.That(_service.RegisterMessage(1, 0, true), Is.Null);
        }

        Assert.That(_service.IsMuted(1, out _), Is.False);
    }

    [Test]
    public void Clear_RemovesMute()
    {
        for (var i = 0; i < 10; i++)
        {
            _service.RegisterMessage(1, 0, false);
        }

        _service.Clear(1);

        Assert.That(_service.IsMuted(1, out _), Is.False);
    }

    private int CountMessagesUntilMute(long playerId, int chatProtection)
    {
        for (var i = 1; i <= 20; i++)
        {
            if (_service.RegisterMessage(playerId, chatProtection, false) != null)
            {
                return i;
            }
        }

        return 20;
    }
}
