using System.Collections.Concurrent;
using Ada.API.Interfaces.Game.Rooms;
using Ada.Db;
using Microsoft.EntityFrameworkCore;

namespace Ada.Game.Rooms.Filter;

public class RoomWordFilterService(IDbContextFactory<AdaDbContext> dbContextFactory) : IRoomWordFilterService
{
    private readonly ConcurrentDictionary<int, string[]> _cache = new();

    public async Task<bool> ContainsFilteredWordAsync(int roomId, string message)
    {
        var words = _cache.TryGetValue(roomId, out var cached)
            ? cached
            : await LoadAsync(roomId);

        if (words.Length == 0)
        {
            return false;
        }

        var lowered = message.ToLowerInvariant();

        return words.Any(word => lowered.Contains(word, StringComparison.Ordinal));
    }

    public void Invalidate(int roomId) => _cache.TryRemove(roomId, out _);

    private async Task<string[]> LoadAsync(int roomId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var words = await dbContext.RoomWordFilters
            .Where(x => x.RoomId == roomId)
            .Select(x => x.Word)
            .ToArrayAsync();

        _cache[roomId] = words;

        return words;
    }
}
