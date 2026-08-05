using Ada.API.Collections;

namespace Ada.Tests.API.Collections;

[TestFixture]
public class RevisionedCollectionTests
{
    [Test]
    public void Revision_ChangesOnChurnThatLeavesTheCountIdentical()
    {
        var collection = new RevisionedCollection<string> { "a" };
        var before = collection.Revision;

        collection.Remove("a");
        collection.Add("b");

        Assert.Multiple(() =>
        {
            Assert.That(collection, Has.Count.EqualTo(1));
            Assert.That(collection.Revision, Is.Not.EqualTo(before));
        });
    }

    [Test]
    public void Remove_MissingItem_LeavesRevisionAlone()
    {
        var collection = new RevisionedCollection<string> { "a" };
        var before = collection.Revision;

        Assert.Multiple(() =>
        {
            Assert.That(collection.Remove("absent"), Is.False);
            Assert.That(collection.Revision, Is.EqualTo(before));
        });
    }

    [Test]
    public void Snapshot_IsUnaffectedByLaterMutation()
    {
        var collection = new RevisionedCollection<string> { "a" };
        var snapshot = collection.GetSnapshot();

        collection.Add("b");

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Items, Has.Count.EqualTo(1));
            Assert.That(snapshot.Revision, Is.Not.EqualTo(collection.Revision));
        });
    }

    [Test]
    public void SnapshotOf_PairsTheItemsWithTheRevisionTheyCameFrom()
    {
        var collection = new RevisionedCollection<string> { "a" };
        var (revision, items) = CollectionRevision.SnapshotOf(collection);

        collection.Add("b");

        Assert.Multiple(() =>
        {
            Assert.That(items, Has.Count.EqualTo(1));
            Assert.That(revision, Is.EqualTo(collection.Revision - 1));
        });
    }

    [Test]
    public void SnapshotOf_PlainCollection_FallsBackToCountAndCopies()
    {
        var list = new List<string> { "a" };
        var (revision, items) = CollectionRevision.SnapshotOf(list);

        list.Add("b");

        Assert.Multiple(() =>
        {
            Assert.That(revision, Is.EqualTo(1));
            Assert.That(items, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Enumeration_ConcurrentWithMutation_DoesNotThrow()
    {
        var collection = new RevisionedCollection<int>();

        for (var i = 0; i < 200; i++)
        {
            collection.Add(i);
        }

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var writer = Task.Run(() =>
        {
            var next = 1000;

            while (!cancellation.IsCancellationRequested)
            {
                collection.Add(next++);
                collection.Remove(next - 1);
            }
        });

        Assert.DoesNotThrow(() =>
        {
            while (!cancellation.IsCancellationRequested)
            {
                _ = collection.Count(x => x >= 0);
            }
        });

        writer.Wait(TimeSpan.FromSeconds(5));
    }
}
