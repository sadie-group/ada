using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Shared.Extensions;
using Ada.Networking.Writers.Rooms.Users.HandItems;

using Microsoft.Extensions.Logging;

namespace Ada.Game.Rooms.Furniture.Interactors;

public class VendingInteractor(IRoomTileMapHelperService tileMapHelperService,
    IRoomFurnitureItemHelperService roomFurnitureItemHelperService,
    ILogger<VendingInteractor> logger) : AbstractRoomFurnitureItemInteractor
{
    public override List<string> InteractionTypes => [FurnitureItemInteractionType.VendingMachine];
    
    public override async Task OnTriggerAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser)
    {
        var direction = tileMapHelperService.GetOppositeDirection(item.Direction);

        roomUser.Direction = direction;
        roomUser.DirectionHead = direction;
        roomUser.NeedsUpdate = true;

        var handItems = item
            .PlayerFurnitureItem
            .FurnitureItem
            .HandItems
            .ToList();

        var squareInFront = tileMapHelperService.GetPointInFront(item.PositionX, item.PositionY, item.Direction);
        
        if (handItems.Count < 1 || roomUser.Point != squareInFront)
        {
            roomUser.WalkToPoint(squareInFront, OnReachedGoal);
            return;

            void OnReachedGoal()
                => OnTriggerAsync(room, item, roomUser)
                    .FireAndForget(logger, "vending walk-to-goal trigger");
        }
        
        await roomFurnitureItemHelperService.UpdateMetaDataForItemAsync(room, item, "1");
        await Task.Delay(500);
        await roomFurnitureItemHelperService.UpdateMetaDataForItemAsync(room, item, "0");

        var handItem = handItems.PickRandom();

        roomUser.HandItemId = handItem.Id;
        roomUser.HandItemSet = DateTime.Now;
        
        await room.BroadcastDataAsync(new RoomUserHandItemWriter
        {
            UserId = roomUser.Player.Player.Id,
            ItemId = handItem.Id
        });
    }
}