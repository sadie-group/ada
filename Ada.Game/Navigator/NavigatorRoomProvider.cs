using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Navigator;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.Db;
using Ada.Db.Models.Rooms;
using Ada.Game.Navigator.Filterers;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Game.Navigator;

public class NavigatorRoomProvider(
    IRoomRepository roomRepository, 
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IEnumerable<INavigatorSearchFilterer> filterers,
    IMapper mapper) : INavigatorRoomProvider
{
    public async Task<List<RoomDto>> GetRoomsForCategoryNameAsync(IPlayerLogic player, string category)
    {
        return category switch
        {
            "popular" => await GetPopularRoomsAsync(100),
            "official" => await GetOfficialRoomsAsync(100),
            "my_rooms" => await GetPlayerRoomsAsync(player.Player.Id),
            _ => []
        };
    }
    
    private async Task<List<RoomDto>> GetPopularRoomsAsync(int amount)
    {
        var rooms = await QueryRoomsAsync(query => query.OrderByDescending(x => x.PlayerLikes.Count)
            .ThenByDescending(x => x.CreatedAt)
            .Take(amount));

        return rooms
            .OrderByDescending(x => roomRepository.TryGetRoomById(x.Id)?.UserRepository.Count ?? 0)
            .ToList();
    }
    
    private async Task<List<RoomDto>> GetOfficialRoomsAsync(int amount)
    {
        return await QueryRoomsAsync(query => query
            .Where(x => x.Owner!.Roles.Any(r => r.Name != "User"))
            .OrderByDescending(x => x.PlayerLikes.Count)
            .Take(amount));
    }

    private async Task<List<RoomDto>> GetPlayerRoomsAsync(long playerId)
    {
        return await QueryRoomsAsync(query => query.Where(x => x.OwnerId == playerId));
    }
    
    
    private async Task<List<RoomDto>> QueryRoomsAsync(Func<IQueryable<Room>, IQueryable<Room>> shape)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var rooms = await shape(dbContext
                .Set<Room>()
                .AsNoTracking()
                .AsSplitQuery())
            .Include(x => x.Settings)
            .Include(x => x.Layout)
            .Include(x => x.PaintSettings)
            .Include(x => x.ChatSettings)
            .Include(x => x.Tags)
            .Include(x => x.PlayerLikes)
            .Include(x => x.Group)
            .Include(x => x.DimmerSettings)
            .ToListAsync();

        return mapper.Map<List<RoomDto>>(rooms);
    }

    public async Task<List<RoomDto>> GetRoomsForSearchQueryAsync(string searchQuery)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        var query = dbContext
            .Rooms
            .AsQueryable();
        
        if (searchQuery.Contains(':'))
        {
            query = ApplyFilter(query, searchQuery.Split([':'], 2));
        }
        else
        {
            query = query.Where(x =>
                x.Name.Contains(searchQuery) ||
                x.Description.Contains(searchQuery) ||
                x.Tags.Any(t => t.Name.Contains(searchQuery)) ||
                x.Owner!.Username.Contains(searchQuery));
        }

        var rooms = await query
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Settings)
            .Include(x => x.Layout)
            .Include(x => x.Owner)
            .Include(x => x.PaintSettings)
            .Include(x => x.ChatSettings)
            .Include(x => x.PlayerLikes)
            .Include(x => x.Tags)
            .Include(x => x.Group)
            .Include(x => x.DimmerSettings)
            .Take(100)
            .ToListAsync();
        
        return mapper.Map<List<RoomDto>>(rooms);
    }

    private IQueryable<Room> ApplyFilter(IQueryable<Room> query, IReadOnlyList<string> filterData)
    {
        var filterer = filterers.FirstOrDefault(x => x.Name == filterData[0]);

        return filterer != null ? 
            filterer.Apply(query, filterData[1]) : 
            query;
    }
}