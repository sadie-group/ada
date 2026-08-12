namespace Ada.API.Interfaces.Game.Rooms;

public interface IRoomWordFilterService
{
    Task<bool> ContainsFilteredWordAsync(int roomId, string message);
    void Invalidate(int roomId);
}
