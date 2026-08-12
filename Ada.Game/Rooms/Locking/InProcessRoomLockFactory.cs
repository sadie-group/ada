using Ada.API.Interfaces.Game.Rooms;

namespace Ada.Game.Rooms.Locking;

public sealed class InProcessRoomLockFactory : IRoomLockFactory
{
    public IRoomLock Create(int roomId) => new InProcessRoomLock();
}
