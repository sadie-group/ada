namespace Ada.API.Interfaces.Game.Players;

public interface IPlayerUnseenItems
{
    void Add(int category, IEnumerable<int> itemIds);
    void Remove(int category, IEnumerable<int> itemIds);
    void ClearCategory(int category);
    IReadOnlyList<int> GetCategory(int category);
}
