using System.Collections.Immutable;
using Ada.API;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Bots;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Pathfinding;
using Ada.API.Interfaces.Game.Rooms.Pets;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Networking;
using Ada.Db.Models.Rooms;
using Ada.Networking.Packets.Serialization;

namespace Ada.Game.Rooms;

public class RoomLogic(
    RoomDto room,
    IRoomTileMap tileMap,
    IRoomPathFinder pathFinder,
    IRoomUserRepository userRepository,
    IRoomBotRepository botRepository,
    IRoomPetRepository petRepository,
    IRoomLock roomLock)
    : Room, IRoomLogic
{
    public RoomDto Room { get; } = room;
    public IRoomTileMap TileMap { get; } = tileMap;
    public IRoomPathFinder PathFinder { get; } = pathFinder;
    public IRoomUserRepository UserRepository { get; } = userRepository;
    public IRoomBotRepository BotRepository { get; } = botRepository;
    public IRoomPetRepository PetRepository { get; } = petRepository;

    private static readonly AsyncLocal<ImmutableHashSet<RoomLogic>?> _heldRooms = new();

    private sealed class LockAcquisition
    {
        public int ActiveReentrantBodies;
    }

    private static readonly AsyncLocal<LockAcquisition?> _acquisition = new();
    private static readonly AsyncLocal<int> _reentrancyDepth = new();

    public async Task RunLockedAsync(Func<Task> action)
    {
        var held = _heldRooms.Value ?? ImmutableHashSet<RoomLogic>.Empty;

        if (held.Contains(this))
        {
            await RunReentrantAsync(action);
            return;
        }

        await roomLock.AcquireAsync();

        var acquisition = _acquisition.Value;
        var depth = _reentrancyDepth.Value;

        _heldRooms.Value = held.Add(this);
        _acquisition.Value = new LockAcquisition();
        _reentrancyDepth.Value = 0;

        try
        {
            await action();
        }
        finally
        {
            _heldRooms.Value = held;
            _acquisition.Value = acquisition;
            _reentrancyDepth.Value = depth;
            roomLock.Release();
        }
    }

    private async Task RunReentrantAsync(Func<Task> action)
    {
        var acquisition = _acquisition.Value;

        if (acquisition == null)
        {
            await action();
            return;
        }

        var depthBefore = _reentrancyDepth.Value;
        var active = Interlocked.Increment(ref acquisition.ActiveReentrantBodies);

        if (active > depthBefore + 1)
        {
            ConcurrentReentryDetected?.Invoke(Room.Id, active);
        }

        _reentrancyDepth.Value = depthBefore + 1;

        try
        {
            await action();
        }
        finally
        {
            _reentrancyDepth.Value = depthBefore;
            Interlocked.Decrement(ref acquisition.ActiveReentrantBodies);
        }
    }

    public static event Action<int, int>? ConcurrentReentryDetected;

    public long LockHeldForMilliseconds => roomLock.HeldForMilliseconds;

    private volatile bool _disposed;

    public bool IsDisposed => _disposed;

    public async ValueTask DisposeAsync()
    {
        await RunLockedAsync(async () =>
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            await UserRepository.DisposeAsync();
            await BotRepository.DisposeAsync();
            await PetRepository.DisposeAsync();
        });
    }

    public Task BroadcastDataAsync(AbstractPacketWriter writer, IReadOnlyCollection<long>? excludedIds = null)
    {
        var recipients = ResolveRecipients(excludedIds);

        if (recipients != null)
        {
            PacketBroadcast.SendAndFlush(writer, recipients);
        }

        return Task.CompletedTask;
    }

    public void QueueBroadcast(AbstractPacketWriter writer, IReadOnlyCollection<long>? excludedIds = null)
    {
        var recipients = ResolveRecipients(excludedIds);

        if (recipients != null)
        {
            PacketBroadcast.Queue(writer, recipients);
        }
    }

    public void FlushQueuedBroadcasts()
    {
        foreach (var user in UserRepository.GetAll())
        {
            user.NetworkObject.FlushAsync();
        }
    }

    private IReadOnlyList<INetworkObject>? ResolveRecipients(IReadOnlyCollection<long>? excludedIds)
    {
        if (excludedIds is not { Count: > 0 })
        {
            var all = UserRepository.GetNetworkObjects();

            return all.Count == 0 ? null : all;
        }

        var users = UserRepository.GetAll();

        if (users.Count == 0)
        {
            return null;
        }

        var excluded = excludedIds as IReadOnlySet<long> ?? new HashSet<long>(excludedIds);
        var recipients = new List<INetworkObject>(users.Count);

        foreach (var user in users)
        {
            if (excluded.Contains(user.Player.Player.Id))
            {
                continue;
            }

            recipients.Add(user.NetworkObject);
        }

        return recipients.Count == 0 ? null : recipients;
    }
}
