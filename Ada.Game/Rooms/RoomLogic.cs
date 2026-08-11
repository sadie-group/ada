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

    private static readonly AsyncLocal<ImmutableHashSet<RoomLogic>?> HeldRooms = new();

    private sealed class LockAcquisition
    {
        public int ActiveReentrantBodies;
    }

    private static readonly AsyncLocal<LockAcquisition?> Acquisition = new();
    private static readonly AsyncLocal<int> ReentrancyDepth = new();

    public async Task RunLockedAsync(Func<Task> action)
    {
        var held = HeldRooms.Value ?? ImmutableHashSet<RoomLogic>.Empty;

        if (held.Contains(this))
        {
            await RunReentrantAsync(action);
            return;
        }

        await roomLock.AcquireAsync();

        var acquisition = Acquisition.Value;
        var depth = ReentrancyDepth.Value;

        HeldRooms.Value = held.Add(this);
        Acquisition.Value = new LockAcquisition();
        ReentrancyDepth.Value = 0;

        try
        {
            await action();
        }
        finally
        {
            HeldRooms.Value = held;
            Acquisition.Value = acquisition;
            ReentrancyDepth.Value = depth;
            roomLock.Release();
        }
    }

    private async Task RunReentrantAsync(Func<Task> action)
    {
        var acquisition = Acquisition.Value;

        if (acquisition == null)
        {
            await action();
            return;
        }

        var depthBefore = ReentrancyDepth.Value;
        var active = Interlocked.Increment(ref acquisition.ActiveReentrantBodies);

        if (active > depthBefore + 1)
        {
            ConcurrentReentryDetected?.Invoke(Room.Id, active);
        }

        ReentrancyDepth.Value = depthBefore + 1;

        try
        {
            await action();
        }
        finally
        {
            ReentrancyDepth.Value = depthBefore;
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

    private List<INetworkObject>? ResolveRecipients(IReadOnlyCollection<long>? excludedIds)
    {
        var users = UserRepository.GetAll();

        if (users.Count == 0)
        {
            return null;
        }

        var excluded = excludedIds is { Count: > 0 }
            ? excludedIds as IReadOnlySet<long> ?? new HashSet<long>(excludedIds)
            : null;

        var recipients = new List<INetworkObject>(users.Count);

        foreach (var user in users)
        {
            if (excluded != null && excluded.Contains(user.Player.Player.Id))
            {
                continue;
            }

            recipients.Add(user.NetworkObject);
        }

        return recipients.Count == 0 ? null : recipients;
    }
}
