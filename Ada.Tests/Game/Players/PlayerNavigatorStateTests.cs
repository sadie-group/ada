using Ada.Game.Players;

namespace Ada.Tests.Game.Players;

[TestFixture]
public class PlayerNavigatorStateTests
{
    [Test]
    public void Collapse_MarksCategoryCollapsed()
    {
        var state = new PlayerNavigatorState();

        Assert.Multiple(() =>
        {
            Assert.That(state.Collapse("staffpicks"), Is.True);
            Assert.That(state.IsCollapsed("staffpicks"), Is.True);
            Assert.That(state.CollapsedCategories, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Collapse_Twice_IsIdempotent()
    {
        var state = new PlayerNavigatorState();
        state.Collapse("staffpicks");

        Assert.Multiple(() =>
        {
            Assert.That(state.Collapse("staffpicks"), Is.False);
            Assert.That(state.CollapsedCategories, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Expand_RemovesCategory()
    {
        var state = new PlayerNavigatorState();
        state.Collapse("staffpicks");

        Assert.Multiple(() =>
        {
            Assert.That(state.Expand("staffpicks"), Is.True);
            Assert.That(state.IsCollapsed("staffpicks"), Is.False);
            Assert.That(state.CollapsedCategories, Is.Empty);
        });
    }

    [Test]
    public void Expand_UnknownCategory_ReturnsFalse()
    {
        Assert.That(new PlayerNavigatorState().Expand("nope"), Is.False);
    }

    [Test]
    public void CategoryNames_AreCaseSensitive()
    {
        var state = new PlayerNavigatorState();
        state.Collapse("staffpicks");

        Assert.That(state.IsCollapsed("StaffPicks"), Is.False);
    }

    [Test]
    public void ListMode_DefaultsToZero()
    {
        Assert.That(new PlayerNavigatorState().GetListMode("staffpicks"), Is.EqualTo(0));
    }

    [Test]
    public void SetListMode_IsStoredPerCategory()
    {
        var state = new PlayerNavigatorState();

        state.SetListMode("staffpicks", 1);

        Assert.Multiple(() =>
        {
            Assert.That(state.GetListMode("staffpicks"), Is.EqualTo(1));
            Assert.That(state.GetListMode("history"), Is.EqualTo(0));
        });
    }
}
