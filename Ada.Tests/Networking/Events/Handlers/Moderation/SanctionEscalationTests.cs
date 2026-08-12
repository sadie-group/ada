using Ada.Networking.Events.Handlers.Moderation;

namespace Ada.Tests.Networking.Events.Handlers.Moderation;

[TestFixture]
public class SanctionEscalationTests
{
    [Test]
    public void MuteExpiry_FirstOffence_IsOneHour()
    {
        var expiry = SanctionEscalation.MuteExpiryFor(0, 1);

        Assert.That((expiry - DateTimeOffset.UtcNow).TotalHours, Is.EqualTo(1).Within(0.05));
    }

    [Test]
    public void MuteExpiry_EscalatesWithPriorSanctions()
    {
        var first = SanctionEscalation.MuteExpiryFor(0, 1);
        var second = SanctionEscalation.MuteExpiryFor(1, 1);
        var third = SanctionEscalation.MuteExpiryFor(2, 1);

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.GreaterThan(first));
            Assert.That(third, Is.GreaterThan(second));
        });
    }

    [Test]
    public void MuteExpiry_ClampsAtTopOfLadder()
    {
        var atTop = SanctionEscalation.MuteExpiryFor(3, 1);
        var beyondTop = SanctionEscalation.MuteExpiryFor(99, 1);

        Assert.That((beyondTop - atTop).TotalSeconds, Is.EqualTo(0).Within(1));
    }

    [Test]
    public void MuteExpiry_NegativePriorCount_IsTreatedAsFirstOffence()
    {
        var negative = SanctionEscalation.MuteExpiryFor(-5, 1);

        Assert.That((negative - DateTimeOffset.UtcNow).TotalHours, Is.EqualTo(1).Within(0.05));
    }

    [Test]
    public void TradeLockExpiry_FirstOffence_IsOneDay()
    {
        var expiry = SanctionEscalation.TradeLockExpiryFor(0, 1);

        Assert.That((expiry - DateTimeOffset.UtcNow).TotalDays, Is.EqualTo(1).Within(0.05));
    }

    [Test]
    public void TradeLockExpiry_EscalatesWithPriorSanctions()
    {
        var first = SanctionEscalation.TradeLockExpiryFor(0, 1);
        var fourth = SanctionEscalation.TradeLockExpiryFor(3, 1);

        Assert.That((fourth - first).TotalDays, Is.EqualTo(29).Within(0.05));
    }
}
