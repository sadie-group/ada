using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Services.Wired;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;

namespace Ada.Game.Rooms.Wired.Conditions;

public class WiredConditionTriggererOnFurnitureStrategy : IWiredConditionStrategy
{
    public virtual string InteractionType => FurnitureItemInteractionType.WiredConditionTriggererOnFurniture;

    public bool IsSatisfied(
        IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto condition,
        IRoomUser? userWhoTriggered)
    {
        return Evaluate(room, condition, userWhoTriggered) != Negate;
    }

    protected virtual bool Negate => false;

    private static bool Evaluate(
        IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto condition,
        IRoomUser? userWhoTriggered)
    {
        var selectedItems = condition.WiredData?.SelectedItems;

        if (userWhoTriggered == null || selectedItems == null || selectedItems.Count == 0)
        {
            return false;
        }

        return selectedItems.Any(selected =>
            room.Room.FurnitureItems.Any(item =>
                item.Id == selected.Id &&
                item.PositionX == userWhoTriggered.Point.X &&
                item.PositionY == userWhoTriggered.Point.Y));
    }
}

public class WiredConditionNotTriggererOnFurnitureStrategy : WiredConditionTriggererOnFurnitureStrategy
{
    public override string InteractionType => FurnitureItemInteractionType.WiredConditionNotTriggererOnFurniture;
    protected override bool Negate => true;
}
