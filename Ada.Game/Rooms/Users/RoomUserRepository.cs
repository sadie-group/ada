using System.Collections.Concurrent;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Networking.Packets.Serialization;
using Ada.Networking.Writers.Rooms;
using Ada.Networking.Writers.Rooms.Bots;
using Ada.Networking.Writers.Rooms.Users;
using Microsoft.Extensions.Logging;

namespace Ada.Game.Rooms.Users;

public class RoomUserRepository(ILogger<RoomUserRepository> logger,
    IPlayerRepository playerRepository,
    IPlayerHelperService playerHelperService) : IRoomUserRepository
{
    private readonly ConcurrentDictionary<long, IRoomUser> _users = new();

    public ICollection<IRoomUser> GetAll() => _users.Values;
    
    public bool TryAdd(IRoomUser user) => _users.TryAdd(user.Player.Player.Id, user);
    
    public bool TryGetById(long id, out IRoomUser? user) => _users.TryGetValue(id, out user);

    public bool TryGetByUsername(string username, out IRoomUser? user)
    {
        user = _users.Values.FirstOrDefault(x => x.Player.Player.Username == username);
        return user != null;
    }
    
    private IRoomLogic _room = null!;
    public DateTime? NoUsersSince { get; set; }

    public void SetRoom(IRoomLogic room)
    {
        _room = room;
    }

    public async Task TryRemoveAsync(
        long id, 
        bool notifyLeft, 
        bool hotelView = false)
    {
        var result = _users.TryRemove(id, out var roomUser);

        if (!result || roomUser == null)
        {
            logger.LogError($"Failed to remove a room user");
            return;
        }
        
        if (notifyLeft)
        {
            var writer = new RoomUserLeftWriter
            {
                UserId = id.ToString()
            };

            await _room.BroadcastDataAsync(writer);
        }
        
        var player = roomUser.Player;
        player.State.CurrentRoomId = 0;
        
        await playerHelperService.UpdatePlayerStatusForFriendsAsync(
            player,
            player.GetMergedFriendships(),
            true, 
            false,
            playerRepository);
        
        if (hotelView)
        {
            await roomUser.NetworkObject.WriteToStreamAsync(new RoomUserHotelViewWriter());
        }
        
        await roomUser.DisposeAsync();
    }
    
    public int Count => _users.Count;

    public ICollection<IRoomUser> GetAllWithRights()
    {
        return _users.Values.Where(x => x.HasRights()).ToList();
    }
    
    public async Task ProcessNewWalkRequestsAsync()
    {
        try
        {
            var users = _users.Values;
            List<IRoomUser>? usersStartedWalking = null;

            foreach (var user in users)
            {
                if (await user.TryStartPendingWalkAsync())
                {
                    (usersStartedWalking ??= []).Add(user);
                }
            }

            if (usersStartedWalking == null)
            {
                return;
            }

            var dataWriter = NetworkPacketWriterSerializer.Serialize(
                new RoomUserDataWriter
                {
                    Users = usersStartedWalking
                });

            var statusWriter = NetworkPacketWriterSerializer.Serialize(
                new RoomUserStatusWriter
                {
                    Users = usersStartedWalking
                });

            foreach (var u in users)
            {
                u.NetworkObject.QueueOutbound(dataWriter);
                u.NetworkObject.QueueOutbound(statusWriter);
            }

            foreach (var u in usersStartedWalking)
            {
                u.NeedsUpdate = false;
            }
        }
        catch (Exception e)
        {
            logger.LogError(e.ToString());
        }
    }

    public async Task RunPeriodicCheckAsync()
    {
        try
        {
            var users = _users.Values;

            if (users.Count == 0)
            {
                NoUsersSince ??= DateTime.UtcNow;
            }
            else
            {
                NoUsersSince = null;
            }
            
            foreach (var user in users)
            {
                await user.RunPeriodicCheckAsync();
            }
        
            if (users.Count > 0 && _room.BotRepository.Count > 0)
            {
                var botsNeedUpdate = _room.BotRepository
                    .GetAll()
                    .Where(x => x.NeedsUpdate)
                    .ToList();

                if (botsNeedUpdate.Count > 0)
                {
                    await _room.BroadcastDataAsync(new RoomBotStatusWriter { Bots = botsNeedUpdate });
                    await _room.BroadcastDataAsync(new RoomBotDataWriter { Bots = botsNeedUpdate });

                    foreach (var bot in botsNeedUpdate)
                    {
                        bot.NeedsUpdate = false;
                    }
                }
            }

            var usersNeedsUpdate = users
                .Where(x => x.NeedsUpdate)
                .ToList();

            if (usersNeedsUpdate.Count != 0)
            {
                var dataWriter = NetworkPacketWriterSerializer.Serialize(
                    new RoomUserDataWriter
                    {
                        Users = usersNeedsUpdate
                    });

                var statusWriter = NetworkPacketWriterSerializer.Serialize(
                    new RoomUserStatusWriter
                    {
                        Users = usersNeedsUpdate
                    });

                foreach (var u in users)
                {
                    u.NetworkObject.QueueOutbound(dataWriter);
                    u.NetworkObject.QueueOutbound(statusWriter);
                }

                foreach (var u in usersNeedsUpdate)
                {
                    u.NeedsUpdate = false;
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e.ToString());
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var user in _users.Values)
        {
            await TryRemoveAsync(user.Player.Player.Id, false);
        }
        
        _users.Clear();
    }
}