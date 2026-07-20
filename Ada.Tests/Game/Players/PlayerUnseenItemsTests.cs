using Ada.Game.Players;

namespace Ada.Tests.Game.Players;

[TestFixture]
public class PlayerUnseenItemsTests
{
    private PlayerUnseenItems _unseenItems;
    private static readonly int[] Expected = [10];

    [SetUp]
    public void Setup()
    {
        _unseenItems = new PlayerUnseenItems();
    }

    [Test]
    public void GetCategory_EmptyByDefault()
    {
        Assert.That(_unseenItems.GetCategory(1), Is.Empty);
    }

    [Test]
    public void Add_TracksItemsPerCategory()
    {
        _unseenItems.Add(1, [10, 20]);
        _unseenItems.Add(2, [30]);

        Assert.Multiple(() =>
        {
            Assert.That(_unseenItems.GetCategory(1), Is.EquivalentTo(new[] { 10, 20 }));
            Assert.That(_unseenItems.GetCategory(2), Is.EquivalentTo(new[] { 30 }));
        });
    }

    [Test]
    public void Add_IgnoresDuplicates()
    {
        _unseenItems.Add(1, [10, 10, 10]);

        Assert.That(_unseenItems.GetCategory(1), Is.EquivalentTo(new[] { 10 }));
    }

    [Test]
    public void Remove_ClearsOnlyGivenItems()
    {
        _unseenItems.Add(1, [10, 20, 30]);
        _unseenItems.Remove(1, [20]);

        Assert.That(_unseenItems.GetCategory(1), Is.EquivalentTo(new[] { 10, 30 }));
    }

    [Test]
    public void Remove_UnknownCategoryDoesNotThrow()
    {
        Assert.DoesNotThrow(() => _unseenItems.Remove(99, [1]));
    }

    [Test]
    public void ClearCategory_RemovesWholeCategoryOnly()
    {
        _unseenItems.Add(1, [10]);
        _unseenItems.Add(2, [20]);

        _unseenItems.ClearCategory(1);

        Assert.Multiple(() =>
        {
            Assert.That(_unseenItems.GetCategory(1), Is.Empty);
            Assert.That(_unseenItems.GetCategory(2), Is.EquivalentTo(new[] { 20 }));
        });
    }

    [Test]
    public void GetCategory_ReturnsSnapshotNotLiveView()
    {
        _unseenItems.Add(1, [10]);
        var snapshot = _unseenItems.GetCategory(1);

        _unseenItems.Add(1, [20]);

        Assert.That(snapshot, Is.EquivalentTo(Expected));
    }
}
