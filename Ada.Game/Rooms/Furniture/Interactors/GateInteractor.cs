using System.Drawing;
using Microsoft.EntityFrameworkCore;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;
using Ada.Db;

namespace Ada.Game.Rooms.Furniture.Interactors;

public class GateInteractor(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomFurnitureItemHelperService roomFurnitureItemHelperService) : AbstractRoomFurnitureItemInteractor
{
    public override List<string> InteractionTypes => [FurnitureItemInteractionType.Gate];
    
    public override async Task OnTriggerAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser)
    {
        if (room.TileMap.UsersAtPoint(new Point(item.PositionX, item.PositionY)) || 
            room
                .UserRepository
                .GetAll()
                .Any(x => x.IsWalking && x.NextPoint == new Point(item.PositionX, item.PositionY)))
        {
            return; 
        }
        
        var newState = item.PlayerFurnitureItem.MetaData == "0" ? 1 : 0;
        
        await roomFurnitureItemHelperService.UpdateMetaDataForItemAsync(
            room, 
            item, 
            newState.ToString());

        room.TileMap.Map[item.PositionY, item.PositionX] = (short) (newState == 1 ? 1 : 0);
    }

    public override async Task OnPlaceAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser)
    {
        await roomFurnitureItemHelperService.UpdateMetaDataForItemAsync(room, item, "0");
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.Entry(item.PlayerFurnitureItem).Property(x => x.MetaData).IsModified = true;
        await dbContext.SaveChangesAsync();
    }
}