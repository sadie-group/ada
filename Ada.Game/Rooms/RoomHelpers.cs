using System.Collections.Concurrent;
using System.Drawing;
using Ada.API.DTOs.Players;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Enums.Game.Rooms;
using Ada.Core.Enums.Miscellaneous;
using Ada.Db;
using Ada.Db.Models.Players;
using Ada.Db.Models.Rooms;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Game.Rooms;

public static class RoomHelpers
{
    private static readonly ConcurrentDictionary<long, Task<IRoomLogic?>> InFlightLoads = new();

    public static Task<IRoomLogic?> TryLoadRoomByIdAsync(
        long id,
        IRoomRepository roomRepository,
        IDbContextFactory<AdaDbContext> dbContextFactory,
        IMapper mapper)
    {
        var memoryValue = roomRepository.TryGetRoomById(id);

        if (memoryValue is { IsDisposed: false })
        {
            return Task.FromResult<IRoomLogic?>(memoryValue);
        }

        if (memoryValue is { IsDisposed: true })
        {
            roomRepository.TryRemove(id, out _);
        }

        return InFlightLoads.GetOrAdd(
            id,
            static (key, state) => LoadRoomAsync(key, state.Repository, state.ContextFactory, state.Mapper),
            (Repository: roomRepository, ContextFactory: dbContextFactory, Mapper: mapper));
    }

    private static async Task<IRoomLogic?> LoadRoomAsync(
        long id,
        IRoomRepository roomRepository,
        IDbContextFactory<AdaDbContext> dbContextFactory,
        IMapper mapper)
    {
        try
        {
            return await LoadRoomCoreAsync(id, roomRepository, dbContextFactory, mapper);
        }
        finally
        {
            InFlightLoads.TryRemove(id, out _);
        }
    }

    private static async Task<IRoomLogic?> LoadRoomCoreAsync(
        long id,
        IRoomRepository roomRepository,
        IDbContextFactory<AdaDbContext> dbContextFactory,
        IMapper mapper)
    {
        var memoryValue = roomRepository.TryGetRoomById(id);

        if (memoryValue is { IsDisposed: false })
        {
            return memoryValue;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var room = await dbContext.Set<Room>()
            .Include(x => x.Layout)
            .Include(x => x.FurnitureItems).ThenInclude(x => x.PlayerFurnitureItem).ThenInclude(x => x.FurnitureItem)
            .Include(x => x.Owner)
            .Include(x => x.PaintSettings)
            .Include(x => x.ChatSettings)
            .Include(x => x.PlayerLikes)
            .Include(x => x.Tags)
            .Include(x => x.Group)
            .Include(x => x.DimmerSettings)
            .Include(x => x.PlayerBans).ThenInclude(x => x.Player)
            .AsSplitQuery()
            .AsNoTrackingWithIdentityResolution()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (room == null)
        {
            return null;
        }

        var roomDto = mapper.Map<RoomDto>(room);
        var roomLogic = mapper.Map<IRoomLogic>(roomDto);

        roomLogic.UserRepository.SetRoom(roomLogic);
        roomLogic = roomRepository.GetOrAddRoom(roomLogic);

        return roomLogic;
    }

    public static bool CanEnterRoom(IRoomLogic room, IPlayerLogic player, out RoomEnterError error)
    {
        error = default;

        if (room.Room.OwnerId == player.Player.Id)
        {
            return true;
        }

        if (room.UserRepository.Count >= room.Room.MaxUsersAllowed)
        {
            error = RoomEnterError.NoCapacity;
            return false;
        }

        if (room.Room.PlayerBans.Any(x =>
                x.PlayerId == player.Player.Id && x.ExpiresAt > DateTimeOffset.Now))
        {
            error = RoomEnterError.Banned;
            return false;
        }

        return true;
    }

    private static RoomControllerLevel GetControllerLevelForUser(IRoomLogic room, IPlayerLogic player)
    {
        var controllerLevel = RoomControllerLevel.None;

        void Grant(RoomControllerLevel level)
        {
            if (level > controllerLevel)
            {
                controllerLevel = level;
            }
        }

        if (room.Room.PlayerRights.FirstOrDefault(x => x.PlayerId == player.Player.Id) != null)
        {
            Grant(RoomControllerLevel.Rights);
        }

        if (room.Room.OwnerId == player.Player.Id)
        {
            Grant(RoomControllerLevel.Owner);
        }

        if (player.HasPermission(PlayerPermissionName.AnyRoomRights))
        {
            Grant(RoomControllerLevel.Owner);
        }

        if (player.HasPermission(PlayerPermissionName.Moderator))
        {
            Grant(RoomControllerLevel.Moderator);
        }

        return controllerLevel;
    }

    public static IRoomUser CreateUserForEntry(
        IRoomUserFactory roomUserFactory,
        IRoomLogic room,
        IPlayerLogic player,
        Point spawnPoint,
        HDirection direction)
    {
        return roomUserFactory.Create(
            room,
            player.NetworkObject!,
            spawnPoint,
            room.TileMap.ZMap[spawnPoint.Y, spawnPoint.X],
            direction,
            direction,
            player,
            GetControllerLevelForUser(room, player));
    }

    public static async Task CreateRoomVisitForPlayerAsync(
        IPlayerLogic player,
        int roomId,
        IDbContextFactory<AdaDbContext> dbContextFactory,
        IMapper mapper)
    {
        var roomVisit = new PlayerRoomVisitDto
        {
            PlayerId = player.Player.Id,
            RoomId = roomId,
            CreatedAt = DateTime.Now
        };

        player.Player.RoomVisits.Add(roomVisit);

        var roomVisitEntity = mapper.Map<PlayerRoomVisit>(roomVisit);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.PlayerRoomVisits.Add(roomVisitEntity);
        await dbContext.SaveChangesAsync();
    }
}
