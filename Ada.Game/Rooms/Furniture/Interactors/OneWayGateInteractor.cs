using System.Drawing;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;
using Ada.Db;
using Microsoft.EntityFrameworkCore;

namespace Ada.Game.Rooms.Furniture.Interactors;

public class OneWayGateInteractor(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomTileMapHelperService tileMapHelperService,
    IRoomFurnitureItemHelperService roomFurnitureItemHelperService) : AbstractRoomFurnitureItemInteractor
{
    public override List<string> InteractionTypes => [FurnitureItemInteractionType.OneWayGate];
    
    public override async Task OnTriggerAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser)
    {
        var squareInFront = tileMapHelperService.GetPointInFront(item.PositionX, item.PositionY, item.Direction);
        
        if (roomUser.Point != squareInFront)
        {
            return;
        }

        var squareBehind = tileMapHelperService.GetPointInFront(item.PositionX, item.PositionY,
            tileMapHelperService.GetOppositeDirection(item.Direction));

        if (!room.TileMap.TileExists(squareBehind))
        {
            return;
        }

        var itemPoint = new Point(item.PositionX, item.PositionY);
        
        await roomFurnitureItemHelperService.UpdateMetaDataForItemAsync(room, item, "1");

        roomUser.DirectionHead = tileMapHelperService.GetOppositeDirection(item.Direction);
        roomUser.Direction = tileMapHelperService.GetOppositeDirection(item.Direction);
        roomUser.NeedsUpdate = true;
        roomUser.OverridePoints.Add(itemPoint);
        roomUser.CanWalk = false;
        roomUser.WalkToPoint(squareBehind, OnReachedGoal);
        
        return;

        async void OnReachedGoal()
        {
            await roomFurnitureItemHelperService.UpdateMetaDataForItemAsync(room, item, "0");

            roomUser.OverridePoints.Remove(itemPoint);
            roomUser.CanWalk = true;
        }
    }

    public override async Task OnPlaceAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser)
    {
        await roomFurnitureItemHelperService.UpdateMetaDataForItemAsync(room, item, "0");

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        await dbContext.PlayerFurnitureItems
            .Where(x => x.Id == item.PlayerFurnitureItem.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.MetaData, item.PlayerFurnitureItem.MetaData));
    }
}