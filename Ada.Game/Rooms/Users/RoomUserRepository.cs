using Ada.API;
﻿using System.Collections.Concurrent;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Networking.Packets.Serialization;
using Ada.Networking.Writers.Rooms.Bots;
using Ada.Networking.Writers.Rooms.Users;
using Ada.Networking.Writers.Rooms.Users.Trading;
using Microsoft.Extensions.Logging;

namespace Ada.Game.Rooms.Users;

public class RoomUserRepository(ILogger<RoomUserRepository> logger,
    IPlayerRepository playerRepository,
    IPlayerHelperService playerHelperService) : IRoomUserRepository
{
    private readonly ConcurrentDictionary<long, IRoomUser> _users = new();

    private readonly object _snapshotLock = new();
    private volatile IRoomUser[] _snapshot = [];
    private volatile INetworkObject[] _networkSnapshot = [];

    private void RebuildSnapshot()
    {
        lock (_snapshotLock)
        {
            var users = _users.Values.ToArray();
            var networkObjects = new INetworkObject[users.Length];

            for (var i = 0; i < users.Length; i++)
            {
                networkObjects[i] = users[i].NetworkObject;
            }

            _snapshot = users;
            _networkSnapshot = networkObjects;
        }
    }

    public ICollection<IRoomUser> GetAll() => _snapshot;

    public IReadOnlyList<INetworkObject> GetNetworkObjects() => _networkSnapshot;

    public bool TryAdd(IRoomUser user)
    {
        if (!_users.TryAdd(user.Player.Player.Id, user))
        {
            return false;
        }

        RebuildSnapshot();
        return true;
    }

    public bool TryGetById(long id, out IRoomUser? user) => _users.TryGetValue(id, out user);

    public bool TryGetByUsername(string username, out IRoomUser? user)
    {
        user = _snapshot.FirstOrDefault(x => x.Player.Player.Username == username);
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
            logger.LogError("Failed to remove a room user");
            return;
        }

        RebuildSnapshot();

        await CancelTradeAsync(roomUser);

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

    private static async Task CancelTradeAsync(IRoomUser roomUser)
    {
        var trade = roomUser.Trade;

        if (trade == null)
        {
            return;
        }

        foreach (var user in trade.Users)
        {
            user.Trade = null;
            user.TradeStatus = 0;
        }

        foreach (var user in trade.Users)
        {
            if (user == roomUser)
            {
                continue;
            }

            try
            {
                await user.NetworkObject.WriteToStreamAsync(new RoomUserTradeCloseWindowWriter());
            }
            catch (Exception)
            {
            }
        }
    }

    public int Count => _users.Count;

    public ICollection<IRoomUser> GetAllWithRights()
    {
        return _snapshot.Where(x => x.HasRights()).ToList();
    }

    public async Task ProcessNewWalkRequestsAsync()
    {
        try
        {
            var users = _snapshot;
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

            var recipients = users.Select(u => u.NetworkObject).ToList();

            PacketBroadcast.Queue(
                new RoomUserStatusWriter
                {
                    Users = usersStartedWalking
                },
                recipients);

            foreach (var u in usersStartedWalking)
            {
                u.NeedsUpdate = false;
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to broadcast walking-user updates");
        }
    }

    public async Task RunPeriodicCheckAsync()
    {
        try
        {
            var users = _snapshot;

            if (users.Length == 0)
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

            if (users.Length > 0 && _room.BotRepository.Count > 0)
            {
                var botsNeedUpdate = _room.BotRepository
                    .GetAll()
                    .Where(x => x.NeedsUpdate)
                    .ToList();

                if (botsNeedUpdate.Count > 0)
                {
                    await _room.BroadcastDataAsync(new RoomBotStatusWriter { Bots = botsNeedUpdate });

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
                var recipients = users.Select(u => u.NetworkObject).ToList();

                PacketBroadcast.Queue(
                    new RoomUserStatusWriter
                    {
                        Users = usersNeedsUpdate
                    },
                    recipients);

                foreach (var u in usersNeedsUpdate)
                {
                    u.NeedsUpdate = false;
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Room periodic user check failed");
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
