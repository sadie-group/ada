namespace Ada.API.Interfaces.Game.Rooms;

public interface IRoomLock
{
    ValueTask AcquireAsync();

    void Release();
}
