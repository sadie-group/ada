using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.Core.Enums.Game.Furniture;

namespace Ada.Game.Rooms.Wired.Effects;

public class WiredEffectFleeStrategy(
    IRoomTileMapHelperService tileMapHelperService,
    IRoomFurnitureItemHelperService furnitureItemHelperService)
    : AbstractWiredMovementEffectStrategy(tileMapHelperService, furnitureItemHelperService)
{
    public override string InteractionType => FurnitureItemInteractionType.WiredEffectFleeFromClosestUser;

    protected override async Task MoveItemAsync(
        IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto effect,
        PlayerFurnitureItemPlacementDataDto item)
    {
        var closestUser = GetClosestUser(room, item);

        if (closestUser == null)
        {
            return;
        }

        await TryMoveInDirectionAsync(
            room,
            item,
            GetOppositeDirection(
                GetDirectionTowards(new(item.PositionX, item.PositionY), closestUser.Point)));
    }
}
