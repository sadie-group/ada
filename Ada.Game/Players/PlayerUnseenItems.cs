using Ada.API.Interfaces.Game.Players;

namespace Ada.Game.Players;

public class PlayerUnseenItems : IPlayerUnseenItems
{
    private readonly Dictionary<int, HashSet<int>> _itemIdsByCategory = [];
    private readonly Lock _mutex = new();

    public void Add(int category, IEnumerable<int> itemIds)
    {
        lock (_mutex)
        {
            if (!_itemIdsByCategory.TryGetValue(category, out var itemIdSet))
            {
                itemIdSet = [];
                _itemIdsByCategory[category] = itemIdSet;
            }

            foreach (var itemId in itemIds)
            {
                itemIdSet.Add(itemId);
            }
        }
    }

    public void Remove(int category, IEnumerable<int> itemIds)
    {
        lock (_mutex)
        {
            if (!_itemIdsByCategory.TryGetValue(category, out var itemIdSet))
            {
                return;
            }

            foreach (var itemId in itemIds)
            {
                itemIdSet.Remove(itemId);
            }

            if (itemIdSet.Count == 0)
            {
                _itemIdsByCategory.Remove(category);
            }
        }
    }

    public void ClearCategory(int category)
    {
        lock (_mutex)
        {
            _itemIdsByCategory.Remove(category);
        }
    }

    public IReadOnlyList<int> GetCategory(int category)
    {
        lock (_mutex)
        {
            return _itemIdsByCategory.TryGetValue(category, out var itemIdSet) ? [.. itemIdSet] : [];
        }
    }
}
