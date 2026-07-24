using System.Drawing;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Furniture;

[PacketId(EventHandlerId.RoomCloseDice)]
public class RoomCloseDiceEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomTileMapHelperService tileMapHelperService,
    IRoomFurnitureItemHelperService roomFurnitureItemHelperService) : INetworkPacketEventHandler
{
    public required int ItemId { get; init; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        var room = client
            .RoomUser
            .Room;
        
        var roomFurnitureItem = room
            .Room.FurnitureItems
            .FirstOrDefault(x => x.Id == ItemId);

        if (roomFurnitureItem == null || roomFurnitureItem.PlayerFurnitureItem.MetaData == "-1")
        {
            return;
        }

        var itemPosition = new Point(roomFurnitureItem.PositionX, roomFurnitureItem.PositionY);
        
        if (tileMapHelperService.GetSquaresBetweenPoints(itemPosition, client.RoomUser.Point) > 1)
        {
            return;
        }

        await roomFurnitureItemHelperService.UpdateMetaDataForItemAsync(room, roomFurnitureItem, "0");
        
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.Entry(roomFurnitureItem.PlayerFurnitureItem).Property(x => x.MetaData).IsModified = true;
        await dbContext.SaveChangesAsync();
    }
}