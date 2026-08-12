using Ada.Networking.Client;

namespace Ada.Tests.Networking.Client;

[TestFixture]
public class WalkRequestThrottleTests
{
    [Test]
    public void TryConsume_WithinBurst_Allows()
    {
        var throttle = new WalkRequestThrottle();

        for (var i = 0; i < 12; i++)
        {
            Assert.That(throttle.TryConsume(1), Is.True, $"request {i} should be allowed");
        }
    }

    [Test]
    public void TryConsume_BeyondBurst_Rejects()
    {
        var throttle = new WalkRequestThrottle();

        for (var i = 0; i < 12; i++)
        {
            throttle.TryConsume(1);
        }

        Assert.That(throttle.TryConsume(1), Is.False);
    }

    [Test]
    public void TryConsume_SeparatePlayers_HaveSeparateBudgets()
    {
        var throttle = new WalkRequestThrottle();

        for (var i = 0; i < 12; i++)
        {
            throttle.TryConsume(1);
        }

        Assert.Multiple(() =>
        {
            Assert.That(throttle.TryConsume(1), Is.False);
            Assert.That(throttle.TryConsume(2), Is.True);
        });
    }

    [Test]
    public void Forget_ResetsBudget()
    {
        var throttle = new WalkRequestThrottle();

        for (var i = 0; i < 12; i++)
        {
            throttle.TryConsume(1);
        }

        Assert.That(throttle.TryConsume(1), Is.False);

        throttle.Forget(1);

        Assert.That(throttle.TryConsume(1), Is.True);
    }
}
