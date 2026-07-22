using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Shared;

namespace Ada.Game.Rooms.Furniture.Interactors;

public class RandomStateInteractor(
    IRoomFurnitureItemHelperService roomFurnitureItemHelperService)
    : AbstractRoomFurnitureItemInteractor
{
    public override List<string> InteractionTypes => [
        FurnitureItemInteractionType.RandomState
    ];

    public override async Task OnTriggerAsync(IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto item,
        IRoomUser roomUser)
    {
        var states = item.PlayerFurnitureItem.FurnitureItem.InteractionModes;

        if (states < 2)
        {
            return;
        }

        await roomFurnitureItemHelperService.UpdateMetaDataForItemAsync(room, item, "");

        await Task.Delay(500);

        await roomFurnitureItemHelperService.UpdateMetaDataForItemAsync(room,
            item,
            GlobalState.Random.Next(0, states).ToString());
    }
}
