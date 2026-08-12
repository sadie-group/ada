using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Services.Wired;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;

namespace Ada.Game.Rooms.Wired.Effects;

public class WiredEffectToggleFurnitureStateStrategy(
    IRoomFurnitureItemHelperService furnitureItemHelperService) : IWiredEffectStrategy
{
    public string InteractionType => FurnitureItemInteractionType.WiredEffectToggleFurnitureState;

    public async Task ExecuteAsync(
        IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto effect,
        IRoomUser? userWhoTriggered)
    {
        if (effect.WiredData == null)
        {
            return;
        }

        foreach (var selectedItem in effect.WiredData.SelectedItems)
        {
            var roomItem = room.Room.FurnitureItems.FirstOrDefault(x => x.Id == selectedItem.Id);

            if (roomItem == null)
            {
                continue;
            }

            await furnitureItemHelperService.CycleInteractionStateForItemAsync(room, roomItem);
        }
    }
}
