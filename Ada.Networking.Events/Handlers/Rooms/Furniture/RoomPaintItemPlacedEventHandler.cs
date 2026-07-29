using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Rooms.Furniture;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Writers.Players.Inventory;
using Ada.Networking.Writers.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Furniture;

[PacketId(EventHandlerId.RoomPaintItemPlaced)]
public class RoomPaintItemPlacedEventHandler(
    IRoomRepository roomRepository,
    IDbContextFactory<AdaDbContext> dbContextFactory) : INetworkPacketEventHandler
{
    public int ItemId { get; init; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null || client.RoomUser == null)
        {
            await FurniturePlacementErrorSender.SendAsync(client, RoomFurniturePlacementError.CantSetItem);
            return;
        }

        if (!client.RoomUser.HasRights())
        {
            await FurniturePlacementErrorSender.SendAsync(client, RoomFurniturePlacementError.MissingRights);
            return;
        }
        
        var room = roomRepository.TryGetRoomById(client.Player.State.CurrentRoomId);
        
        if (room == null)
        {
            await FurniturePlacementErrorSender.SendAsync(client, RoomFurniturePlacementError.CantSetItem);
            return;
        }
        
        var player = client.Player;
        var playerItem = player.Player.FurnitureItems.FirstOrDefault(x => x.Id == ItemId);

        if (playerItem == null)
        {
            await FurniturePlacementErrorSender.SendAsync(client, RoomFurniturePlacementError.CantSetItem);
            return;
        }
        
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        switch (playerItem.FurnitureItem.AssetName)
        {
            case "floor":
                room.Room.PaintSettings.FloorPaint = playerItem.MetaData;
                await dbContext.RoomPaintSettings
                    .Where(x => x.RoomId == room.Room.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.FloorPaint, playerItem.MetaData));
                break;
            case "wallpaper":
                room.Room.PaintSettings.WallPaint = playerItem.MetaData;
                await dbContext.RoomPaintSettings
                    .Where(x => x.RoomId == room.Room.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.WallPaint, playerItem.MetaData));
                break;
            case "landscape":
                room.Room.PaintSettings.LandscapePaint = playerItem.MetaData;
                await dbContext.RoomPaintSettings
                    .Where(x => x.RoomId == room.Room.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.LandscapePaint, playerItem.MetaData));
                break;
        }

        player.Player.FurnitureItems.Remove(playerItem);

        await dbContext.PlayerFurnitureItems
            .Where(x => x.Id == playerItem.Id)
            .ExecuteDeleteAsync();
        
        await client.WriteToStreamAsync(new PlayerInventoryRemoveItemWriter
        {
            ItemId = ItemId
        });
        
        await room.BroadcastDataAsync(new RoomPaintWriter
        {
            Type = playerItem.FurnitureItem.AssetName,
            Value = playerItem.MetaData
        });
    }
}