using Ada.API.DTOs.Rooms;

namespace Ada.API.Interfaces.Game.Rooms;

public interface IRoomRepository
{
    IRoomLogic? TryGetRoomById(long id);
    void AddRoom(IRoomLogic roomLogic);
    IRoomLogic GetOrAddRoom(IRoomLogic roomLogic);
    List<RoomDto> GetPopularRooms(int amount);
    int Count { get; }
    IEnumerable<IRoomLogic> GetAllRooms();
    bool TryRemove(long id, out IRoomLogic? roomLogic);
    ValueTask DisposeAsync();
}
