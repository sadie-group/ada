using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.Core.Enums.Game.Furniture;

namespace Ada.Game.Rooms.Wired.Effects;

public class WiredEffectMoveToDirectionStrategy(
    IRoomTileMapHelperService tileMapHelperService,
    IRoomFurnitureItemHelperService furnitureItemHelperService)
    : AbstractWiredMovementEffectStrategy(tileMapHelperService, furnitureItemHelperService)
{
    public override string InteractionType => FurnitureItemInteractionType.WiredEffectChangeFurnitureDirection;

    protected override async Task MoveItemAsync(
        IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto effect,
        PlayerFurnitureItemPlacementDataDto item)
    {
        if (await TryMoveInDirectionAsync(room, item, item.Direction))
        {
            return;
        }

        var opposite = GetOppositeDirection(item.Direction);

        if (await TryMoveInDirectionAsync(room, item, opposite))
        {
            item.Direction = opposite;
            await furnitureItemHelperService.BroadcastItemUpdateToRoomAsync(room, item);
        }
    }
}
