using Ada.API.DTOs.Players.Furniture;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;
using Ada.Db;
using Ada.Db.Models.Rooms;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Game.Rooms.Furniture.Interactors;

public class DimmerInteractor(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IMapper mapper) : AbstractRoomFurnitureItemInteractor
{
    public override List<string> InteractionTypes => [FurnitureItemInteractionType.Dimmer];
    
    public override async Task OnPlaceAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser)
    {
        if (room.Room.DimmerSettings == null)
        {
            var presetOne = new RoomDimmerPreset
            {
                RoomId = room.Room.Id,
                PresetId = 1,
                BackgroundOnly = false,
                Color = "",
                Intensity = 255
            };

            var presetTwo = new RoomDimmerPreset
            {
                RoomId = room.Room.Id,
                PresetId = 2,
                BackgroundOnly = false,
                Color = "",
                Intensity = 255
            };

            var presetThree = new RoomDimmerPreset
            {
                RoomId = room
                    .Room.Id,
                PresetId = 3,
                BackgroundOnly = false,
                Color = "",
                Intensity = 255
            };
            
            room.Room.DimmerSettings = new RoomDimmerSettingsDto()
            {
                RoomId = room.Room.Id,
                Enabled = false,
                PresetId = 1
            };

            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            
            dbContext.RoomDimmerPresets.Add(presetOne);
            dbContext.RoomDimmerPresets.Add(presetTwo);
            dbContext.RoomDimmerPresets.Add(presetThree);
            
            var dimmerSettings = mapper.Map<RoomDimmerSettings>(room.Room.DimmerSettings);
            dbContext.RoomDimmerSettings.Add(dimmerSettings);

            await dbContext.SaveChangesAsync();
        }
    }

    public override async Task OnPickUpAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        await dbContext
            .RoomDimmerPresets
            .Where(x => x.RoomId == room.Room.Id)
            .ExecuteDeleteAsync();
     
        if (room.Room.DimmerSettings != null)
        {
            room.Room.DimmerSettings = null;
            
            dbContext.Entry(room.Room.DimmerSettings).State = EntityState.Deleted;
            await dbContext.SaveChangesAsync();
        }
    }
}