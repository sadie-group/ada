using Ada.Core.Shared.Game.Achievements;

namespace Ada.Tests.Core;

[TestFixture]
public class AchievementDefinitionTests
{
    private static AchievementDefinition Sample()
        => new("ACH_Test", 1, [
            new AchievementLevel(1, 10, "b1"),
            new AchievementLevel(5, 20, "b2"),
            new AchievementLevel(10, 30, "b3")
        ]);

    [Test]
    public void Resolve_NoProgress_IsLevelZeroTargetingFirstLevel()
    {
        var state = Sample().Resolve(0);

        Assert.Multiple(() =>
        {
            Assert.That(state.CurrentLevel, Is.EqualTo(0));
            Assert.That(state.ProgressForNextLevel, Is.EqualTo(1));
            Assert.That(state.RewardPoints, Is.EqualTo(10));
            Assert.That(state.Completed, Is.False);
        });
    }

    [Test]
    public void Resolve_MidProgress_ReportsCurrentAndNextLevel()
    {
        var state = Sample().Resolve(6);

        Assert.Multiple(() =>
        {
            Assert.That(state.CurrentLevel, Is.EqualTo(2));
            Assert.That(state.ProgressForCurrentLevel, Is.EqualTo(5));
            Assert.That(state.ProgressForNextLevel, Is.EqualTo(10));
            Assert.That(state.RewardPoints, Is.EqualTo(30));
            Assert.That(state.Completed, Is.False);
        });
    }

    [Test]
    public void Resolve_AtOrBeyondMax_IsCompleted()
    {
        var state = Sample().Resolve(50);

        Assert.Multiple(() =>
        {
            Assert.That(state.CurrentLevel, Is.EqualTo(3));
            Assert.That(state.MaxLevel, Is.EqualTo(3));
            Assert.That(state.Completed, Is.True);
        });
    }

    [Test]
    public void Definitions_AreRegisteredAndFindable()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AchievementDefinitions.All, Is.Not.Empty);
            Assert.That(AchievementDefinitions.Find("ACH_Login"), Is.Not.Null);
            Assert.That(AchievementDefinitions.Find("does_not_exist"), Is.Null);
        });
    }
}
