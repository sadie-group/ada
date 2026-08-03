using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Networking;
using Ada.Db;
using Ada.Networking.Packets.Serialization;
using Ada.Networking.Writers.Rooms.Users.Trading;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events;

public class RoomUserTrade(
    IPlayerHelperService playerHelperService,
    IDbContextFactory<AdaDbContext> dbContextFactory) : IRoomUserTrade
{
    public required List<IRoomUser> Users { get; init; }
    public required List<PlayerFurnitureItemDto> Items { get; init; }
    
    public async Task OfferItemsAsync(List<PlayerFurnitureItemDto> playerItems)
    {
        foreach (var item in playerItems.Where(item => !Items.Contains(item)))
        {
            Items.Add(item);
        }

        foreach (var user in Users)
        {
            user.TradeStatus = 0;
        }

        await BroadcastToUsersAsync(new RoomUserTradeUpdateWriter
        {
            Trade = this
        });
    }
    
    public Task BroadcastToUsersAsync(AbstractPacketWriter writer)
    {
        return PacketBroadcast.SendAsync(writer, Users.Select(user => user.NetworkObject));
    }
    
    public async Task SwapItemsAsync()
    {
        var map = new Dictionary<long, List<PlayerFurnitureItemDto>>();
        
        foreach (var item in Items)
        {
            if (!map.TryGetValue(item.PlayerId, out var value))
            {
                value = [];
                map[item.PlayerId] = value;
            }

            value.Add(item);
        }

        var userOne = Users[0].Player;
        var userTwo = Users[1].Player;

        var userOneItems = map.TryGetValue(userOne.Player.Id, out var oneItems) ? 
            oneItems : [];
        
        var userTwoItems = map.TryGetValue(userTwo.Player.Id, out var twoItems) ? 
            twoItems : [];
        
        var updateMap = new Dictionary<IPlayerLogic, List<PlayerFurnitureItemDto>>();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        foreach (var userOneItem in userOneItems)
        {
            userOneItem.PlayerId = userTwo.Player.Id;

            userOne.Player.FurnitureItems.Remove(userOneItem);
            userTwo.Player.FurnitureItems.Add(userOneItem);

            if (!updateMap.ContainsKey(userTwo))
            {
                updateMap[userTwo] = [];
            }

            updateMap[userTwo].Add(userOneItem);
        }
        
        foreach (var userTwoItem in userTwoItems)
        {
            userTwoItem.PlayerId = userOne.Player.Id;

            userTwo.Player.FurnitureItems.Remove(userTwoItem);
            userOne.Player.FurnitureItems.Add(userTwoItem);

            if (!updateMap.ContainsKey(userOne))
            {
                updateMap[userOne] = [];
            }

            updateMap[userOne].Add(userTwoItem);
        }

        foreach (var (updatePlayer, updatedItems) in updateMap)
        {
            await playerHelperService.SendUnseenInventoryItemsAsync(updatePlayer, updatedItems);
            await playerHelperService.RefreshInventoryAsync(updatePlayer);
        }

        if (userOneItems.Count > 0)
        {
            var itemIds = userOneItems.Select(x => x.Id).ToList();

            await dbContext.PlayerFurnitureItems
                .Where(x => itemIds.Contains(x.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.PlayerId, userTwo.Player.Id));
        }

        if (userTwoItems.Count > 0)
        {
            var itemIds = userTwoItems.Select(x => x.Id).ToList();

            await dbContext.PlayerFurnitureItems
                .Where(x => itemIds.Contains(x.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.PlayerId, userOne.Player.Id));
        }
    }

    public void RemoveOfferedItem(PlayerFurnitureItemDto item)
    {
        Items.Remove(item);
    }
}