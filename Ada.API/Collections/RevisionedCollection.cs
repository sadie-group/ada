using System.Collections;

namespace Ada.API.Collections;

public sealed class RevisionedCollection<T> : ICollection<T>
{
    public sealed class Snapshot
    {
        public required int Revision { get; init; }
        public required IReadOnlyList<T> Items { get; init; }
    }

    private readonly Lock _gate = new();
    private readonly List<T> _items = [];

    private volatile Snapshot _snapshot = new() { Revision = 0, Items = [] };

    public int Revision => _snapshot.Revision;

    public int Count => _snapshot.Items.Count;

    public bool IsReadOnly => false;

    public Snapshot GetSnapshot() => _snapshot;

    public void Add(T item)
    {
        lock (_gate)
        {
            _items.Add(item);
            Publish();
        }
    }

    public void AddRange(IEnumerable<T> items)
    {
        lock (_gate)
        {
            var before = _items.Count;

            _items.AddRange(items);

            if (_items.Count != before)
            {
                Publish();
            }
        }
    }

    public bool Remove(T item)
    {
        lock (_gate)
        {
            if (!_items.Remove(item))
            {
                return false;
            }

            Publish();
            return true;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            if (_items.Count == 0)
            {
                return;
            }

            _items.Clear();
            Publish();
        }
    }

    public bool Contains(T item) => _snapshot.Items.Contains(item);

    public void CopyTo(T[] array, int arrayIndex)
    {
        var items = _snapshot.Items;

        for (var i = 0; i < items.Count; i++)
        {
            array[arrayIndex + i] = items[i];
        }
    }

    public IEnumerator<T> GetEnumerator() => _snapshot.Items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void Publish() =>
        _snapshot = new Snapshot
        {
            Revision = _snapshot.Revision + 1,
            Items = _items.ToArray()
        };
}

public static class CollectionRevision
{
    public const int Untracked = -1;

    public static (int Revision, IReadOnlyList<T> Items) SnapshotOf<T>(ICollection<T> items)
    {
        if (items is RevisionedCollection<T> revisioned)
        {
            var snapshot = revisioned.GetSnapshot();
            return (snapshot.Revision, snapshot.Items);
        }

        return (Untracked, items.ToArray());
    }

    public static bool IsCurrent<T>(ICollection<T> items, int revision) =>
        items is RevisionedCollection<T> revisioned && revision == revisioned.Revision;
}
