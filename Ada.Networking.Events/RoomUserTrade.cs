using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Networking;
using Ada.Db;
using Ada.Networking.Packets.Serialization;
using Ada.Networking.Writers.Rooms.Users.Trading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ada.Networking.Events;

public class RoomUserTrade(
    IPlayerHelperService playerHelperService,
    IDbContextFactory<AdaDbContext> dbContextFactory,
    ILogger<RoomUserTrade> logger) : IRoomUserTrade
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
        PacketBroadcast.SendAndFlush(writer, Users.Select(user => user.NetworkObject));

        return Task.CompletedTask;
    }

    public async Task<bool> SwapItemsAsync()
    {
        var map = new Dictionary<long, List<PlayerFurnitureItemDto>>();

        var userOne = Users[0].Player;
        var userTwo = Users[1].Player;

        foreach (var item in Items)
        {
            var owner = item.PlayerId == userOne.Player.Id ? userOne :
                item.PlayerId == userTwo.Player.Id ? userTwo : null;

            if (owner == null ||
                item.PlacementData != null ||
                !owner.Player.FurnitureItems.Contains(item))
            {
                return false;
            }

            if (!map.TryGetValue(item.PlayerId, out var value))
            {
                value = [];
                map[item.PlayerId] = value;
            }

            value.Add(item);
        }

        var userOneItems = map.TryGetValue(userOne.Player.Id, out var oneItems) ?
            oneItems : [];

        var userTwoItems = map.TryGetValue(userTwo.Player.Id, out var twoItems) ?
            twoItems : [];

        var updateMap = new Dictionary<IPlayerLogic, List<PlayerFurnitureItemDto>>();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        if (!await TryTransferOwnershipAsync(dbContext, userOneItems, userOne.Player.Id, userTwo.Player.Id) ||
            !await TryTransferOwnershipAsync(dbContext, userTwoItems, userTwo.Player.Id, userOne.Player.Id))
        {
            await transaction.RollbackAsync();
            return false;
        }

        await transaction.CommitAsync();

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
            try
            {
                await playerHelperService.SendUnseenInventoryItemsAsync(updatePlayer, updatedItems);
                await playerHelperService.RefreshInventoryAsync(updatePlayer);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to refresh inventory for player {PlayerId} after a completed trade",
                    updatePlayer.Player.Id);
            }
        }

        return true;
    }

    private static async Task<bool> TryTransferOwnershipAsync(
        AdaDbContext dbContext,
        List<PlayerFurnitureItemDto> items,
        long fromPlayerId,
        long toPlayerId)
    {
        if (items.Count == 0)
        {
            return true;
        }

        var itemIds = items.Select(x => x.Id).Distinct().ToList();

        var updated = await dbContext.PlayerFurnitureItems
            .Where(x => itemIds.Contains(x.Id) && x.PlayerId == fromPlayerId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.PlayerId, toPlayerId));

        return updated == itemIds.Count;
    }

    public void RemoveOfferedItem(PlayerFurnitureItemDto item)
    {
        Items.Remove(item);
    }
}
