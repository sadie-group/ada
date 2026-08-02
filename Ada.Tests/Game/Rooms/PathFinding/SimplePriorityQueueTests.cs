using Ada.Game.Rooms.PathFinding.ToGo.Collections.PriorityQueue;

namespace Ada.Tests.Game.Rooms.PathFinding;

public class SimplePriorityQueueTests
{
    [Test]
    public void Push_FirstItem_ReturnsRootIndex()
    {
        var queue = new SimplePriorityQueue<int>();

        Assert.That(queue.Push(5), Is.EqualTo(0));
        Assert.That(queue.Count, Is.EqualTo(1));
    }

    [Test]
    public void Push_SmallerItem_BubblesToRoot()
    {
        var queue = new SimplePriorityQueue<int>();
        queue.Push(10);
        queue.Push(20);

        Assert.That(queue.Push(1), Is.EqualTo(0));
        Assert.That(queue.Peek(), Is.EqualTo(1));
    }

    [Test]
    public void Push_LargerItem_StaysAtLeaf()
    {
        var queue = new SimplePriorityQueue<int>();
        queue.Push(1);

        Assert.That(queue.Push(10), Is.EqualTo(1));
        Assert.That(queue.Peek(), Is.EqualTo(1));
    }

    [Test]
    public void Pop_ReturnsItemsInPriorityOrder()
    {
        var queue = new SimplePriorityQueue<int>();
        foreach (var value in new[] { 5, 3, 8, 1, 9, 2, 7, 4, 6, 0 })
        {
            queue.Push(value);
        }

        var popped = new List<int>();
        while (queue.Count > 0)
        {
            popped.Add(queue.Pop());
        }

        Assert.That(popped, Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
    }

    [Test]
    public void Pop_DuplicateValues_ReturnsAll()
    {
        var queue = new SimplePriorityQueue<int>();
        foreach (var value in new[] { 2, 1, 2, 1, 3 })
        {
            queue.Push(value);
        }

        var popped = new List<int>();
        while (queue.Count > 0)
        {
            popped.Add(queue.Pop());
        }

        Assert.That(popped, Is.EqualTo(new[] { 1, 1, 2, 2, 3 }));
    }

    [Test]
    public void Pop_Empty_Throws()
    {
        var queue = new SimplePriorityQueue<int>();

        Assert.Throws<ArgumentOutOfRangeException>(() => queue.Pop());
    }

    [Test]
    public void Peek_Empty_ReturnsDefault()
    {
        Assert.That(new SimplePriorityQueue<string>().Peek(), Is.Null);
        Assert.That(new SimplePriorityQueue<int>().Peek(), Is.EqualTo(0));
    }

    [Test]
    public void Peek_DoesNotRemoveItem()
    {
        var queue = new SimplePriorityQueue<int>();
        queue.Push(3);
        queue.Push(1);

        Assert.That(queue.Peek(), Is.EqualTo(1));
        Assert.That(queue.Count, Is.EqualTo(2));
    }

    [Test]
    public void Clear_RemovesAllItems()
    {
        var queue = new SimplePriorityQueue<int>();
        queue.Push(1);
        queue.Push(2);

        queue.Clear();

        Assert.That(queue.Count, Is.EqualTo(0));
    }

    [Test]
    public void Indexer_Get_ReturnsElementAtHeapIndex()
    {
        var queue = new SimplePriorityQueue<int>();
        queue.Push(1);
        queue.Push(2);

        Assert.That(queue[0], Is.EqualTo(1));
        Assert.That(queue[1], Is.EqualTo(2));
    }

    [Test]
    public void Indexer_Set_SmallerValue_BubblesUp()
    {
        var queue = new SimplePriorityQueue<int>();
        foreach (var value in new[] { 1, 5, 6, 7, 8 })
        {
            queue.Push(value);
        }

        queue[4] = 0;

        Assert.That(queue.Peek(), Is.EqualTo(0));
    }

    [Test]
    public void Indexer_Set_LargerValueAtRoot_BubblesDown()
    {
        var queue = new SimplePriorityQueue<int>();
        foreach (var value in new[] { 1, 2, 3, 4, 5, 6, 7 })
        {
            queue.Push(value);
        }

        queue[0] = 100;

        Assert.That(queue.Peek(), Is.EqualTo(2));

        var popped = new List<int>();
        while (queue.Count > 0)
        {
            popped.Add(queue.Pop());
        }

        Assert.That(popped, Is.EqualTo(new[] { 2, 3, 4, 5, 6, 7, 100 }));
    }

    [Test]
    public void Indexer_Set_UnchangedValue_KeepsPosition()
    {
        var queue = new SimplePriorityQueue<int>();
        foreach (var value in new[] { 1, 2, 3 })
        {
            queue.Push(value);
        }

        queue[1] = 2;

        Assert.That(queue[1], Is.EqualTo(2));
        Assert.That(queue.Peek(), Is.EqualTo(1));
    }

    [Test]
    public void CustomComparer_ControlsPriority()
    {
        var queue = new SimplePriorityQueue<int>(Comparer<int>.Create((a, b) => b.CompareTo(a)));
        foreach (var value in new[] { 3, 1, 2 })
        {
            queue.Push(value);
        }

        Assert.That(queue.Pop(), Is.EqualTo(3));
        Assert.That(queue.Pop(), Is.EqualTo(2));
        Assert.That(queue.Pop(), Is.EqualTo(1));
    }
}
