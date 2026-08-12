namespace Ada.API.Interfaces.Game.Rooms;

public interface IRoomLockFactory
{
    IRoomLock Create(int roomId);
}
