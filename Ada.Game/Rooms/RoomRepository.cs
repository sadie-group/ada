using System.Collections.Concurrent;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Rooms;
using Ada.Db;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Game.Rooms;

public class RoomRepository(
    IDbContextFactory<AdaDbContext> dbContextFactory, 
    IMapper mapper) : IRoomRepository
{
    private readonly ConcurrentDictionary<long, IRoomLogic> _rooms = new();

    public IRoomLogic? TryGetRoomById(long id)
    {
        return _rooms.GetValueOrDefault(id);
    }

    public void AddRoom(IRoomLogic roomLogic) => _rooms[roomLogic.Room.Id] = roomLogic;

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
    public IEnumerable<IRoomLogic> GetAllRooms() => _rooms.Values;

    public bool TryRemove(long id, out IRoomLogic? roomLogic)
    {
        return _rooms.TryRemove(id, out roomLogic);
    }
    
    public async ValueTask DisposeAsync()
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        foreach (var room in _rooms.Values)
        {
            dbContext.Entry(room).State = EntityState.Modified;
        }

        await dbContext.SaveChangesAsync();

        foreach (var room in _rooms.Values)
        {
            await room.DisposeAsync();
        }

        _rooms.Clear();
    }
}