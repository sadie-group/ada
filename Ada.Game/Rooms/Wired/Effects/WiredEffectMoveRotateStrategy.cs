using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Enums.Miscellaneous;
using Ada.Core.Shared.Helpers;

namespace Ada.Game.Rooms.Wired.Effects;

public class WiredEffectMoveRotateStrategy(
    IRoomTileMapHelperService tileMapHelperService,
    IRoomFurnitureItemHelperService furnitureItemHelperService)
    : AbstractWiredMovementEffectStrategy(tileMapHelperService, furnitureItemHelperService)
{
    public override string InteractionType => FurnitureItemInteractionType.WiredEffectMoveRotateFurniture;

    protected override async Task MoveItemAsync(
        IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto effect,
        PlayerFurnitureItemPlacementDataDto item)
    {
        var parameters = WiredParameterHelpers.Deserialize(effect.WiredData?.IntParameters);
        var movement = parameters.Count > 0 ? parameters[0] : 0;
        var rotation = parameters.Count > 1 ? parameters[1] : 0;

        var moved = false;

        if (movement > 0)
        {
            var direction = (HDirection)(Random.Shared.Next(4) * 2);
            moved = await TryMoveInDirectionAsync(room, item, direction);
        }

        if (rotation > 0)
        {
            var step = rotation switch
            {
                2 => -2,
                3 => Random.Shared.Next(2) == 0 ? 2 : -2,
                _ => 2
            };

            item.Direction = (HDirection)(((int)item.Direction + step + 8) % 8);
            await FurnitureItemHelperService.BroadcastItemUpdateToRoomAsync(room, item);
        }
        else if (!moved)
        {
            await FurnitureItemHelperService.BroadcastItemUpdateToRoomAsync(room, item);
        }
    }
}
