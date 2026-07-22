using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;

namespace Ada.Game.Rooms.Furniture.Interactors;

public class MultiHeightInteractor(
    IRoomFurnitureItemHelperService furnitureItemHelperService,
    IRoomTileMapHelperService tileMapHelperService) : AbstractRoomFurnitureItemInteractor
{
    public override List<string> InteractionTypes => [
        FurnitureItemInteractionType.MultiHeight
    ];

    public override async Task OnTriggerAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser)
    {
        var heights = item.PlayerFurnitureItem.FurnitureItem.GetMultiHeights();

        if (heights.Count == 0)
        {
            return;
        }

        // Items stacked on top pin the height in place.
        var hasItemOnTop = tileMapHelperService
            .GetItemsForPosition(item.PositionX, item.PositionY, room.Room.FurnitureItems)
            .Any(x => x.Id != item.Id && x.PositionZ > item.PositionZ);

        if (hasItemOnTop)
        {
            return;
        }

        var index = int.TryParse(item.PlayerFurnitureItem.MetaData, out var parsed) ? parsed : 0;
        var nextIndex = (Math.Abs(index) + 1) % heights.Count;

        await furnitureItemHelperService.UpdateMetaDataForItemAsync(room, item, nextIndex.ToString());

        foreach (var user in room.UserRepository.GetAll())
        {
            var onItem = tileMapHelperService
                .GetItemsForPosition(user.Point.X, user.Point.Y, room.Room.FurnitureItems)
                .Any(x => x.Id == item.Id);

            if (onItem)
            {
                user.CheckStatusForCurrentTile();
            }
        }
    }
}
