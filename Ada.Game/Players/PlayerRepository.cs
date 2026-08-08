using System.Collections.Concurrent;
using System.Diagnostics;
using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking;
using Ada.Db;
using Ada.Core.Enums.Game.Players;
using Ada.Db.Models.Players;
using Ada.Networking.Packets.Serialization;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ada.Game.Players;

public class PlayerRepository(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    ILogger<PlayerRepository> logger,
    IMapper mapper) : IPlayerRepository
{
    private const int _slowLookupWarningMs = 300;

    private readonly ConcurrentDictionary<long, IPlayerLogic> _players = new();
    private readonly ConcurrentDictionary<long, string> _playerIdToUsernameCache = new();

    private readonly ConcurrentDictionary<string, IPlayerLogic> _playersByUsername =
        new(StringComparer.OrdinalIgnoreCase);

    public IPlayerLogic? GetPlayerLogicById(long id) => _players.GetValueOrDefault(id);
    public IPlayerLogic? GetPlayerLogicByUsername(string username) =>
        _playersByUsername.GetValueOrDefault(username);

    public async Task<PlayerDto?> GetPlayerByIdAsync(long id)
    {
        if (_players.TryGetValue(id, out var byId))
        {
            return byId.Player;
        }

        var sw = Stopwatch.StartNew();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var player = await dbContext
            .Set<Player>()
            .Include(x => x.Data)
            .Include(x => x.AvatarData)
            .Include(x => x.OriginRelationships).ThenInclude(x => x.TargetPlayer)
            .Include(x => x.Bans)
            .Include(x => x.GameSettings)
            .Include(x => x.NavigatorSettings)
            .Include(x => x.Subscriptions).ThenInclude(x => x.Subscription)
            .Include(x => x.OutgoingFriendships).ThenInclude(x => x.TargetPlayer).ThenInclude(x => x!.AvatarData)
            .Include(x => x.IncomingFriendships).ThenInclude(x => x.OriginPlayer).ThenInclude(x => x!.AvatarData)
            .Include(x => x.Roles)
            .Include(x => x.OutgoingIgnores)
            .Include(x => x.RoomLikes)
            .Include(x => x.FurnitureItems).ThenInclude(x => x.FurnitureItem)
            .Include(x => x.FurnitureItems).ThenInclude(x => x.PlacementData)
            .AsSplitQuery()
            .AsNoTrackingWithIdentityResolution()
            .FirstOrDefaultAsync(x => x.Id == id);

        sw.Stop();

        if (player == null)
        {
            return null;
        }

        if (sw.Elapsed.TotalMilliseconds > _slowLookupWarningMs)
        {
            logger.LogWarning(
                "Loading player {PlayerId} ({Username}) took {ElapsedMs}ms",
                id,
                player.Username,
                sw.Elapsed.TotalMilliseconds);
        }

        return mapper.Map<PlayerDto>(player);
    }

    public async Task<PlayerDto?> GetPlayerByUsernameAsync(string username)
    {
        var online = _playersByUsername.GetValueOrDefault(username);

        if (online != null)
        {
            return mapper.Map<PlayerDto>(online);
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var player = await dbContext
            .Set<Player>()
            .AsNoTracking()
            .Include(x => x.Data)
            .FirstOrDefaultAsync(x => x.Username == username);

        return mapper.Map<PlayerDto>(player);
    }

    public async Task<int> GetAcceptedFriendshipCountAsync(long playerId)
    {
        if (_players.TryGetValue(playerId, out var online))
        {
            return online.GetAcceptedFriendshipCount();
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        return await dbContext.Set<PlayerFriendship>()
            .CountAsync(x =>
                (x.OriginPlayerId == playerId || x.TargetPlayerId == playerId) &&
                x.Status == PlayerFriendshipStatus.Accepted);
    }

    public ICollection<IPlayerLogic> GetAll() => _players.Values;

    public bool TryAddPlayer(IPlayerLogic player)
    {
        if (!_players.TryAdd(player.Player.Id, player))
        {
            return false;
        }

        _playersByUsername[player.Player.Username] = player;

        return true;
    }

    public async Task<bool> TryRemovePlayerAsync(long playerId)
    {
        var result = _players.TryRemove(playerId, out var player);

        if (player == null)
        {
            return result;
        }

        _playersByUsername.TryRemove(
            new KeyValuePair<string, IPlayerLogic>(player.Player.Username, player));

        await player.DisposeAsync();

        return result;
    }

    public long Count()
    {
        return _players.Count;
    }

    public async Task<List<PlayerDto>> GetPlayersForSearchAsync(string searchQuery, long[] excludeIds)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var players = await dbContext
            .Set<Player>()
            .AsNoTracking()
            .Include(x => x.AvatarData)
            .Where(x =>
                x.Username.Contains(searchQuery) &&
                !excludeIds.Contains(x.Id))
            .ToListAsync();

        return mapper.Map<List<PlayerDto>>(players);
    }

    public async Task<List<PlayerRelationshipDto>> GetRelationshipsForPlayerAsync(long playerId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var playerRelationships = await dbContext
            .Set<PlayerRelationship>()
            .AsNoTracking()
            .Where(x => x.OriginPlayerId == playerId || x.TargetPlayerId == playerId)
            .ToListAsync();

        return mapper.Map<List<PlayerRelationshipDto>>(playerRelationships);
    }

    public Task BroadcastDataAsync(AbstractPacketWriter writer)
    {
        PacketBroadcast.SendAndFlush(writer, _players.Values.Select(player => player.NetworkObject!));

        return Task.CompletedTask;
    }

    public async Task<string?> GetPlayerUsernameByIdAsync(long playerId)
    {
        if (_playerIdToUsernameCache.TryGetValue(playerId, out var username))
        {
            return username;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        username = await dbContext.Players
            .Where(x => x.Id == playerId)
            .Select(x => x.Username)
            .FirstOrDefaultAsync();

        if (!string.IsNullOrEmpty(username))
        {
            _playerIdToUsernameCache[playerId] = username;
        }

        return username;
    }

    public async Task<Dictionary<long, string>> GetPlayerUsernamesByIdsAsync(IEnumerable<long> playerIds)
    {
        var resolved = new Dictionary<long, string>();
        var missing = new List<long>();

        foreach (var playerId in playerIds.Distinct())
        {
            if (_playerIdToUsernameCache.TryGetValue(playerId, out var cached))
            {
                resolved[playerId] = cached;
                continue;
            }

            missing.Add(playerId);
        }

        if (missing.Count == 0)
        {
            return resolved;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var rows = await dbContext.Players
            .AsNoTracking()
            .Where(x => missing.Contains(x.Id))
            .Select(x => new { x.Id, x.Username })
            .ToListAsync();

        foreach (var row in rows)
        {
            if (string.IsNullOrEmpty(row.Username))
            {
                continue;
            }

            _playerIdToUsernameCache[row.Id] = row.Username;
            resolved[row.Id] = row.Username;
        }

        return resolved;
    }
}
