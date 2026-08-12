using System.Collections.Concurrent;
using Ada.API.Interfaces.Game.Players;

namespace Ada.Game.Players;

public class PlayerNavigatorState : IPlayerNavigatorState
{
    private readonly ConcurrentDictionary<string, bool> _collapsed = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, int> _listModes = new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> CollapsedCategories => _collapsed.Keys.ToArray();

    public bool Collapse(string category) => _collapsed.TryAdd(category, true);

    public bool Expand(string category) => _collapsed.TryRemove(category, out _);

    public bool IsCollapsed(string category) => _collapsed.ContainsKey(category);

    public int GetListMode(string category) => _listModes.GetValueOrDefault(category);

    public void SetListMode(string category, int mode) => _listModes[category] = mode;
}
