namespace Ada.API.Interfaces.Game.Players;

public interface IPlayerNavigatorState
{
    IReadOnlyCollection<string> CollapsedCategories { get; }

    bool Collapse(string category);
    bool Expand(string category);
    bool IsCollapsed(string category);

    int GetListMode(string category);
    void SetListMode(string category, int mode);
}
