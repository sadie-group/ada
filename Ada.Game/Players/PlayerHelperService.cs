using Ada.API;
using Ada.API.DTOs.Players;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Players.Friendships;
using Ada.API.Interfaces.Game.Players.Packets.Writers;
using Ada.Core.Enums.Game.Players;
using Ada.Game.Players.Packets.Writers;
using Ada.Networking.Events.Dtos;
using Ada.Networking.Packets.Serialization;
using Ada.Networking.Writers.Players;
using Ada.Networking.Writers.Players.Friendships;
using Ada.Networking.Writers.Players.Inventory;
using Ada.Networking.Writers.Players.Subscriptions;

namespace Ada.Game.Players;

public class PlayerHelperService : IPlayerHelperService
{
    public async Task SendFriendUpdatesToPlayerAsync(
        IPlayerLogic player, 
        List<IPlayerFriendshipUpdate> updates)
    {
        await player.NetworkObject!.WriteToStreamAsync(new PlayerUpdateFriendWriter
        {
            Updates = updates
        });
    }

    public async Task SendPlayerFriendListUpdate(
        IPlayerLogic player, 
        IPlayerRepository playerRepository)
    {
        var friends = player
            .GetMergedFriendships()
            .Where(x => x.Status == PlayerFriendshipStatus.Accepted)
            .ToList();

        var pages = friends.Count / 500 + (friends.Count > 0 ? 1 : 0);
        
        for (var i = 0; i < pages; i++)
        {
            var batch = friends.Skip(i * 500).
                Take(500).
                ToList();
            
            await player.NetworkObject!.WriteToStreamAsync(new PlayerFriendsListWriter
            {
                Pages = pages,
                Index = i,
                PlayerId = player.Player.Id,
                Friends = batch,
                PlayerRepository = playerRepository,
                Relationships = player.Player.OriginRelationships
            });
        }
    }
    
    public IPlayerSubscriptionWriter? GetSubscriptionWriterAsync(IPlayerLogic player, string name)
    {
        var playerSub = player.Player.Subscriptions.FirstOrDefault(x => x.Subscription?.Name == name);
        
        if (playerSub?.Subscription == null)
        {
            return null;
        }
        
        var tillExpire = playerSub.ExpiresAt - playerSub.CreatedAt;
        var daysLeft = (int) tillExpire.TotalDays;
        var minutesLeft = (int) tillExpire.TotalMinutes;
        var lastMod = player.State.LastSubscriptionModification;

        return new PlayerSubscriptionWriter
        {
            Name = playerSub.Subscription.Name?.ToLower() ?? string.Empty,
            DaysLeft = daysLeft,
            MemberPeriods = 1,
            PeriodsSubscribedAhead = 2,
            ResponseType = 0,
            HasEverBeenMember = true,
            IsVip = true,
            PastClubDays = 0,
            PastVipDays = 0,
            MinutesTillExpire = minutesLeft,
            MinutesSinceModified = (int)(DateTime.UtcNow - lastMod).TotalMinutes
        };
    }

    public Task UpdatePlayerStatusForFriendsAsync(
        IPlayerLogic player, 
        IEnumerable<PlayerFriendshipDto> friendships, 
        bool isOnline, 
        bool inRoom,
        IPlayerRepository playerRepository)
    {
        var avatarData = player.Player.AvatarData;

        var update = new PlayerFriendshipUpdate
        {
            Type = 0,
            Friend = new FriendData
            {
                Id = player.Player.Id,
                Username = player.Player.Username,
                FigureCode = avatarData?.FigureCode ?? string.Empty,
                Motto = avatarData?.Motto ?? string.Empty,
                Gender = avatarData?.Gender ?? PlayerAvatarGender.Male
            },
            FriendOnline = isOnline,
            FriendInRoom = inRoom,
            Relation = (int) PlayerRelationshipType.None
        };
        
        var recipients = new List<INetworkObject>();

        foreach (var friend in friendships)
        {
            var targetId = friend.OriginPlayerId == player.Player.Id ?
                friend.TargetPlayerId :
                friend.OriginPlayerId;

            var targetPlayer = playerRepository.GetPlayerLogicById(targetId);

            if (targetPlayer?.NetworkObject != null)
            {
                recipients.Add(targetPlayer.NetworkObject);
            }
        }

        if (recipients.Count == 0)
        {
            return Task.CompletedTask;
        }

        PacketBroadcast.SendAndFlush(
            new PlayerUpdateFriendWriter { Updates = [update] },
            recipients);

        return Task.CompletedTask;
    }

    public async Task SendUnseenInventoryItemsAsync(IPlayerLogic player, List<PlayerFurnitureItemDto> items)
    {
        player.State.UnseenItems.Add(UnseenItemCategory.Furniture, items.Select(x => x.Id));

        await player.NetworkObject!.WriteToStreamAsync(new PlayerInventoryUnseenItemsWriter
        {
            Count = 1,
            Category = UnseenItemCategory.Furniture,
            FurnitureItems = items
        });
    }

    public async Task RefreshInventoryAsync(IPlayerLogic player)
    {
        await player.NetworkObject!.WriteToStreamAsync(new PlayerInventoryRefreshWriter());
    }
}