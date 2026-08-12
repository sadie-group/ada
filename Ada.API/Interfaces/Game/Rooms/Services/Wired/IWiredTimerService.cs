namespace Ada.API.Interfaces.Game.Rooms.Services.Wired;

public interface IWiredTimerService
{
    TimeSpan GetElapsed(long roomId);
    void Reset(long roomId);
    bool TryMarkFired(long roomId, int itemId);
    int RetainOnly(IReadOnlySet<long> liveRoomIds);
}
