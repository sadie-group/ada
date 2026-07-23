using Ada.Core.Players;

namespace Ada.Tests.Players;

[TestFixture]
public class PlayerCurrencyMapperTests
{
    [Test]
    public void FromBalances_MapsBalancesToExpectedCurrencySlots()
    {
        var result = PlayerCurrencyMapper.FromBalances(100, 50, 25);

        Assert.Multiple(() =>
        {
            Assert.That(result[0], Is.EqualTo(100));
            Assert.That(result[5], Is.EqualTo(50));
            Assert.That(result[103], Is.EqualTo(25));
        });
    }

    [Test]
    public void FromBalances_UnusedCurrencySlots_AreZero()
    {
        var result = PlayerCurrencyMapper.FromBalances(100, 50, 25);

        var unusedSlots = result.Keys.Except([0, 5, 103]);
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(11));
            Assert.That(unusedSlots.Select(k => result[k]), Has.All.EqualTo(0));
        });
    }
}
