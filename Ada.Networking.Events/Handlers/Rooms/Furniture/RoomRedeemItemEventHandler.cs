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
            room.Room.OwnerId != client.Player!.Player.Id)
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
        dbContext.Entry(roomFurnitureItem).State = EntityState.Deleted;
        dbContext.Entry(roomFurnitureItem.PlayerFurnitureItem).State = EntityState.Deleted;
        await dbContext.SaveChangesAsync();

        if (assetName.StartsWith("CF_") ||
            assetName.StartsWith("CFC_") ||
            assetName.Contains("_diamond_"))
        {
            var value = int.TryParse(assetName.Split("_")[1], out var amount) ? 
                amount : 
                0;

            if (assetName.StartsWith("PF_"))
            {
                player.Player.Data.PixelBalance += value;
                dbContext.Entry(player.Player.Data).Property(x => x.PixelBalance).IsModified = true;
                
                await client.WriteToStreamAsync(new PlayerActivityPointsBalanceWriter
                {
                    Currencies = PlayerCurrencyMapper.FromBalances(
                        player.Player.Data.PixelBalance,
                        player.Player.Data.SeasonalBalance,
                        player.Player.Data.GotwPoints)
                });
            }
            else
            {
                player.Player.Data.CreditBalance += value;
                dbContext.Entry(player.Player.Data).Property(x => x.CreditBalance).IsModified = true;
                
                await client.WriteToStreamAsync(new PlayerCreditsBalanceWriter
                {
                    Credits = player.Player.Data.CreditBalance
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
                player.Player.Data.SeasonalBalance += points;
                dbContext.Entry(player.Player.Data).Property(x => x.SeasonalBalance).IsModified = true;
            }
            else if (pointsType == 103)
            {
                player.Player.Data.GotwPoints += points;
                dbContext.Entry(player.Player.Data).Property(x => x.SeasonalBalance).IsModified = true;
            }
                
            await client.WriteToStreamAsync(new PlayerActivityPointsBalanceWriter
            {
                Currencies = PlayerCurrencyMapper.FromBalances(
    player.Player.Data.PixelBalance,
    player.Player.Data.SeasonalBalance,
    player.Player.Data.GotwPoints)
            });
        }

        await dbContext.SaveChangesAsync();
    }
}