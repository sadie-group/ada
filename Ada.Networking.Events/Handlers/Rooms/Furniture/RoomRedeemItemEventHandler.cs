using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Players;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Writers.Players.Purse;
using Ada.Networking.Writers.Rooms.Furniture;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Furniture;

[PacketId(EventHandlerId.RedeemItem)]
public class RoomRedeemItemEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory) : INetworkPacketEventHandler
{
    public required int ItemId { get; init; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;
        var room = client.RoomUser!.Room;

        if (player?.NetworkObject == null || 
            client.RoomUser == null || 
            room.Room.OwnerId != player.Player.Id)
        {
            return;
        }

        var data = player.Player.Data;

        if (data == null)
        {
            return;
        }
        
        var roomFurnitureItem = room
            .Room.FurnitureItems
            .FirstOrDefault(x => x.PlayerFurnitureItemId == ItemId);

        if (roomFurnitureItem == null)
        {
            return;
        }

        var allowedPrefixes = new List<string>
        {
            "CF_",
            "CFC_",
            "DF_",
            "PF_"
        };

        var assetName = roomFurnitureItem
            .PlayerFurnitureItem
            .FurnitureItem
            .AssetName;
        
        if (!allowedPrefixes.Any(prefix => assetName.StartsWith(prefix)))
        {
            return;
        }
        
        await room.BroadcastDataAsync(new RoomFloorFurnitureItemRemovedWriter
        {
            Id = roomFurnitureItem.PlayerFurnitureItemId.ToString(),
            Expired = false,
            OwnerId = 0,
            Delay = 0
        });
        
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        room.Room.FurnitureItems.Remove(roomFurnitureItem);

        await dbContext.RoomFurnitureItems
            .Where(x => x.Id == roomFurnitureItem.Id)
            .ExecuteDeleteAsync();

        await dbContext.PlayerFurnitureItems
            .Where(x => x.Id == roomFurnitureItem.PlayerFurnitureItem.Id)
            .ExecuteDeleteAsync();

        if (assetName.StartsWith("CF_") ||
            assetName.StartsWith("CFC_") ||
            assetName.Contains("_diamond_"))
        {
            var value = int.TryParse(assetName.Split("_")[1], out var amount) ? 
                amount : 
                0;

            if (assetName.StartsWith("PF_"))
            {
                data.PixelBalance += value;

                await client.WriteToStreamAsync(new PlayerActivityPointsBalanceWriter
                {
                    Currencies = PlayerCurrencyMapper.FromBalances(
                        data.PixelBalance,
                        data.SeasonalBalance,
                        data.GotwPoints)
                });
            }
            else
            {
                data.CreditBalance += value;

                await client.WriteToStreamAsync(new PlayerCreditsBalanceWriter
                {
                    Credits = data.CreditBalance
                });
            }
        }
        else if (assetName.StartsWith("DF"))
        {
            if (!int.TryParse(assetName.Split("_")[1], out var pointsType) || 
                !int.TryParse(assetName.Split("_")[2], out var points))
            {
                return;
            }

            if (pointsType == 5 || assetName.StartsWith("CF_diamond_"))
            {
                data.SeasonalBalance += points;
            }
            else if (pointsType == 103)
            {
                data.GotwPoints += points;
            }
                
            await client.WriteToStreamAsync(new PlayerActivityPointsBalanceWriter
            {
                Currencies = PlayerCurrencyMapper.FromBalances(
    data.PixelBalance,
    data.SeasonalBalance,
    data.GotwPoints)
            });
        }

        await dbContext.PlayerData
            .Where(x => x.PlayerId == player.Player.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.PixelBalance, data.PixelBalance)
                .SetProperty(x => x.CreditBalance, data.CreditBalance)
                .SetProperty(x => x.SeasonalBalance, data.SeasonalBalance)
                .SetProperty(x => x.GotwPoints, data.GotwPoints));
    }
}