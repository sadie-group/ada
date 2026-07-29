using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Furniture;

[PacketId(EventHandlerId.RoomItemUpdateObjectData)]
public class RoomItemUpdateObjectDataEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomFurnitureItemHelperService roomFurnitureItemHelperService) : INetworkPacketEventHandler
{
    public required int ItemId { get; init; }
    public required Dictionary<string, string> ObjectData { get; init; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null ||
            client.RoomUser == null ||
            !client.RoomUser.HasRights())
        {
            return;
        }

        var roomFurnitureItem = client
            .RoomUser
            .Room
            .Room.FurnitureItems
            .FirstOrDefault(x => x.Id == ItemId);
        
        if (roomFurnitureItem == null)
        {
            return;
        }

        var metaData = string.Join(";", ObjectData.Select(x => $"{x.Key}={x.Value}"));
        await roomFurnitureItemHelperService.UpdateMetaDataForItemAsync(client.RoomUser.Room, roomFurnitureItem, metaData);
        
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        await dbContext.PlayerFurnitureItems
            .Where(x => x.Id == roomFurnitureItem.PlayerFurnitureItem.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.MetaData, roomFurnitureItem.PlayerFurnitureItem.MetaData));
    }
}