using Ada.API.DTOs.Players;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Players.Friendships;
using Ada.API.Interfaces.Game.Players.Packets.Writers;

namespace Ada.API.Interfaces.Game.Players;

public interface IPlayerHelperService
{
    Task SendFriendUpdatesToPlayerAsync(
        IPlayerLogic player, 
        List<IPlayerFriendshipUpdate> updates);

    Task SendPlayerFriendListUpdate(
        IPlayerLogic player, 
        IPlayerRepository playerRepository);

    IPlayerSubscriptionWriter? GetSubscriptionWriterAsync(IPlayerLogic player, string name);

    Task UpdatePlayerStatusForFriendsAsync(
        IPlayerLogic player, 
        IEnumerable<PlayerFriendshipDto> friendships, 
        bool isOnline, 
        bool inRoom,
        IPlayerRepository playerRepository);

    Task SendUnseenInventoryItemsAsync(IPlayerLogic player, List<PlayerFurnitureItemDto> items);
    
    Task RefreshInventoryAsync(IPlayerLogic player);
}