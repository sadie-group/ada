using System.Collections.Concurrent;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Rooms;

namespace Ada.Game.Rooms;

public class RoomRepository : IRoomRepository
{
    private readonly ConcurrentDictionary<long, IRoomLogic> _rooms = new();

    private volatile IRoomLogic[] _snapshot = [];
    private readonly Lock _snapshotLock = new();

    private void RebuildSnapshot()
    {
        lock (_snapshotLock)
        {
            _snapshot = _rooms.Values.ToArray();
        }
    }

    public IRoomLogic? TryGetRoomById(long id) => _rooms.GetValueOrDefault(id);

    public void AddRoom(IRoomLogic roomLogic)
    {
        _rooms[roomLogic.Room.Id] = roomLogic;
        RebuildSnapshot();
    }

    public IRoomLogic GetOrAddRoom(IRoomLogic roomLogic)
    {
        while (true)
        {
            var added = _rooms.GetOrAdd(roomLogic.Room.Id, roomLogic);

            if (ReferenceEquals(added, roomLogic))
            {
                RebuildSnapshot();
                return added;
            }

            if (!added.IsDisposed)
            {
                return added;
            }

            _rooms.TryUpdate(roomLogic.Room.Id, roomLogic, added);
        }
    }

    public List<RoomDto> GetPopularRooms(int amount)
    {
        return _rooms
            .Values
            .Where(x => x.UserRepository.Count > 0)
            .OrderByDescending(x => x.UserRepository.Count)
            .Take(amount)
            .Select(x => x.Room)
            .ToList();
    }

    public int Count => _rooms.Count;
    public IEnumerable<IRoomLogic> GetAllRooms() => _snapshot;

    public bool TryRemove(long id, out IRoomLogic? roomLogic)
    {
        if (!_rooms.TryRemove(id, out roomLogic))
        {
            return false;
        }

        RebuildSnapshot();
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var room in _rooms.Values)
        {
            await room.DisposeAsync();
        }

        _rooms.Clear();
        RebuildSnapshot();
    }
}
