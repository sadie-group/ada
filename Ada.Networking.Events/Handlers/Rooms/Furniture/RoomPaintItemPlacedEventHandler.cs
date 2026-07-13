using Microsoft.EntityFrameworkCore;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Rooms.Furniture;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Writers.Players.Inventory;
using Ada.Networking.Writers.Rooms;

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
                dbContext.Entry(room.Room.PaintSettings).Property(x => x.FloorPaint).IsModified = true;
                break;
            case "wallpaper":
                room.Room.PaintSettings.WallPaint = playerItem.MetaData;
                dbContext.Entry(room.Room.PaintSettings).Property(x => x.WallPaint).IsModified = true;
                break;
            case "landscape":
                room.Room.PaintSettings.LandscapePaint = playerItem.MetaData;
                dbContext.Entry(room.Room.PaintSettings).Property(x => x.LandscapePaint).IsModified = true;
                break;
        }

        player.Player.FurnitureItems.Remove(playerItem);
        await dbContext.SaveChangesAsync();
        
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