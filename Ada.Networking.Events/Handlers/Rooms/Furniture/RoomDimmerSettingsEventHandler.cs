using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Writers.Rooms.Furniture;

namespace Ada.Networking.Events.Handlers.Rooms.Furniture;

[PacketId(EventHandlerId.RoomDimmerSettings)]
public class RoomDimmerSettingsEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository,
    IMapper mapper) : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out _))
        {
            return;
        }

        var dimmer = room
            .Room.FurnitureItems
            .FirstOrDefault(x => x
                .PlayerFurnitureItem
                .FurnitureItem.InteractionType == FurnitureItemInteractionType.Dimmer);

        if (dimmer == null)
        {
            return;
        }

        if (room.Room.DimmerSettings == null)
        {
            throw new Exception("DIMMER_SETTINGS_NULL_WHEN_DIMMER_IN_ROOM");
        }
        
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var dimmerPresets = dbContext
            .RoomDimmerPresets
            .Where(x => x.RoomId == room.Room.Id)
            .ToList();
        
        await client.WriteToStreamAsync(new RoomDimmerSettingsWriter
        {
            DimmerSettings = room.Room.DimmerSettings,
            DimmerPresets = mapper.Map<List<RoomDimmerPresetDto>>(dimmerPresets)
        });
    }
}