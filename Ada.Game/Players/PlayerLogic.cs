using Ada.API;
using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Players;
using Ada.Core.Enums.Game.Players;
using Ada.Networking.Writers.Players;
using Microsoft.Extensions.Logging;

namespace Ada.Game.Players;

public class PlayerLogic(
    ILogger<PlayerLogic> logger,
    PlayerDto player)
    : IPlayerLogic
{
    public PlayerDto Player { get; } = player;
    public INetworkObject? NetworkObject { get; set; }
    public IPlayerState State { get; } = new PlayerState();
    public bool Authenticated { get; set; }
    
    public int GetAcceptedFriendshipCount()
    {
        return Player.IncomingFriendships.Count(x => x.Status == PlayerFriendshipStatus.Accepted) + 
               Player.OutgoingFriendships.Count(x => x.Status == PlayerFriendshipStatus.Accepted);
    }

    public List<PlayerFriendshipDto> GetMergedFriendships()
    {
        return Player.OutgoingFriendships
            .Concat(Player.IncomingFriendships)
            .Where(x => x.Status == PlayerFriendshipStatus.Accepted)
            .ToList();
    }

    public bool IsFriendsWith(int targetId)
    {
        return Player.IncomingFriendships.FirstOrDefault(x =>
                   x.OriginPlayerId == targetId && x.Status == PlayerFriendshipStatus.Accepted) !=
               null 
               ||
               Player.OutgoingFriendships.FirstOrDefault(x =>
                   x.TargetPlayerId == targetId && x.Status == PlayerFriendshipStatus.Accepted) !=
               null;
    }

    public PlayerFriendshipDto? TryGetAcceptedFriendshipFor(long targetId)
    {
        var incoming = Player.IncomingFriendships
            .FirstOrDefault(x => x.OriginPlayerId == targetId && x.Status == PlayerFriendshipStatus.Accepted);

        if (incoming != null)
        {
            return incoming;
        }
        
        return Player.OutgoingFriendships
            .FirstOrDefault(x => x.OriginPlayerId == targetId && x.Status == PlayerFriendshipStatus.Accepted);
    }

    public PlayerFriendshipDto? TryGetFriendshipFor(long targetId)
    {
        var incoming = Player.IncomingFriendships
            .FirstOrDefault(x => x.OriginPlayerId == targetId);

        if (incoming != null)
        {
            return incoming;
        }
        
        return Player.OutgoingFriendships
            .FirstOrDefault(x => x.TargetPlayerId == targetId);
    }

    public void DeleteFriendshipFor(long targetId)
    {
        var incoming = Player.IncomingFriendships
            .FirstOrDefault(x => x.OriginPlayerId == targetId);

        if (incoming != null)
        {
            Player.IncomingFriendships.Remove(incoming);
        }

        var outgoing = Player.OutgoingFriendships
            .FirstOrDefault(x => x.OriginPlayerId == targetId);
        
        if (outgoing != null)
        {
            Player.OutgoingFriendships.Remove(outgoing);
        }
    }

    public bool HasPermission(string name)
    {
        return Player.Roles.Any(r => r.Permissions.Any(x => x.Name == name));
    }

    public ValueTask DisposeAsync()
    {
        logger.LogInformation("Player {Username} has logged out", Player.Username);
        return ValueTask.CompletedTask;
    }

    public bool DeservesReward(string? rewardType, int intervalInSeconds)
    {
        DateTimeOffset? lastRewardAt = null;

        foreach (var log in Player.RewardLogs)
        {
            if (log.Type == rewardType && (lastRewardAt == null || log.CreatedAt > lastRewardAt))
            {
                lastRewardAt = log.CreatedAt;
            }
        }

        return lastRewardAt == null ||
               lastRewardAt < DateTime.Now.AddSeconds(-intervalInSeconds);
    }

    public async Task SendAlertAsync(string message)
    {
        await NetworkObject!.WriteToStreamAsync(new PlayerAlertWriter
        {
            Message = message
        });
    }
}