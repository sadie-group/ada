using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Furniture;

[PacketId(EventHandlerId.RoomDimmerToggle)]
public class RoomDimmerToggleEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository,
    IRoomFurnitureItemHelperService roomFurnitureItemHelperService) : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out _) ||
            room.Room.DimmerSettings == null ||
            client.RoomUser == null ||
            !client.RoomUser.HasRights())
        {
            return;
        }

        var dimmer = room
            .Room.FurnitureItems
            .FirstOrDefault(x => x.PlayerFurnitureItem.FurnitureItem.InteractionType == FurnitureItemInteractionType.Dimmer);

        if (dimmer == null)
        {
            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        var preset = dbContext.RoomDimmerPresets
            .FirstOrDefault(x => x.RoomId == room.Room.Id && x.PresetId == room.Room.DimmerSettings.PresetId);

        if (preset == null)
        {
            return;
        }
        
        room.Room.DimmerSettings.Enabled = !room.Room.DimmerSettings.Enabled;

        var enabled = room.Room.DimmerSettings.Enabled ? 2 : 1;
        var bgOnly = preset.BackgroundOnly ? 2 : 0;
        
        await roomFurnitureItemHelperService.UpdateMetaDataForItemAsync(
            room, 
            dimmer, 
            $"{enabled},{preset.PresetId},{bgOnly},{preset.Color},{preset.Intensity}");
        
        dbContext.Entry(room.Room.DimmerSettings).Property(x => x.Enabled).IsModified = true;
        await dbContext.SaveChangesAsync();
    }
}