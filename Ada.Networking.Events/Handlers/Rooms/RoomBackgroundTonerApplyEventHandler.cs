using Microsoft.EntityFrameworkCore;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Events.Attributes;

namespace Ada.Networking.Events.Handlers.Rooms;

[PacketId(EventHandlerId.RoomBackgroundTonerApply)]
public class RoomBackgroundTonerApplyEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomFurnitureItemHelperService roomFurnitureItemHelperService) : INetworkPacketEventHandler
{
    public required int ItemId { get; init; }
    public required int Hue { get; init; } 
    public required int Saturation { get; init; }
    public required int Brightness { get; init; }
    
    [RequiresRoomRights]
    public async Task HandleAsync(INetworkClient client)
    {
        var roomFurnitureItem = client
            .RoomUser!
            .Room
            .Room
            .FurnitureItems
            .FirstOrDefault(x => x.Id == ItemId);
        
        if (roomFurnitureItem == null)
        {
            return;
        }

        var metaData =
            $"{roomFurnitureItem.PlayerFurnitureItem.MetaData.Split(":")[0]}{Hue % 256}:{Saturation % 256}:{Brightness % 256}:";
        
        await roomFurnitureItemHelperService.UpdateMetaDataForItemAsync(client.RoomUser.Room, roomFurnitureItem, metaData);
        
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.Entry(roomFurnitureItem.PlayerFurnitureItem).Property(x => x.MetaData).IsModified = true;
        await dbContext.SaveChangesAsync();
    }
}