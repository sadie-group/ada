using System.Collections.Concurrent;
using Ada.API.Interfaces.Game.Rooms.Services.Wired;

namespace Ada.Game.Rooms.Wired;

public class WiredTimerService : IWiredTimerService
{
    private readonly ConcurrentDictionary<long, DateTimeOffset> _startedAt = new();
    private readonly ConcurrentDictionary<long, ConcurrentDictionary<int, bool>> _firedItems = new();

    public TimeSpan GetElapsed(long roomId)
    {
        return DateTimeOffset.Now - _startedAt.GetOrAdd(roomId, DateTimeOffset.Now);
    }

    public void Reset(long roomId)
    {
        _startedAt[roomId] = DateTimeOffset.Now;
        _firedItems.TryRemove(roomId, out _);
    }

    public bool TryMarkFired(long roomId, int itemId)
    {
        return _firedItems
            .GetOrAdd(roomId, _ => new ConcurrentDictionary<int, bool>())
            .TryAdd(itemId, true);
    }

    public int RetainOnly(IReadOnlySet<long> liveRoomIds)
    {
        var released = 0;

        foreach (var roomId in _startedAt.Keys)
        {
            if (!liveRoomIds.Contains(roomId) && _startedAt.TryRemove(roomId, out _))
            {
                released++;
            }
        }

        foreach (var roomId in _firedItems.Keys)
        {
            if (!liveRoomIds.Contains(roomId))
            {
                _firedItems.TryRemove(roomId, out _);
            }
        }

        return released;
    }
}
