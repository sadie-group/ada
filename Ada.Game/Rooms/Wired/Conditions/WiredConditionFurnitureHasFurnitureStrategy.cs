using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Services.Wired;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;

namespace Ada.Game.Rooms.Wired.Conditions;

public class WiredConditionFurnitureHasFurnitureStrategy : IWiredConditionStrategy
{
    public virtual string InteractionType => FurnitureItemInteractionType.WiredConditionFurnitureHasFurniture;

    public bool IsSatisfied(
        IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto condition,
        IRoomUser? userWhoTriggered)
    {
        return Evaluate(room, condition) != Negate;
    }

    protected virtual bool Negate => false;

    private static bool Evaluate(IRoomLogic room, PlayerFurnitureItemPlacementDataDto condition)
    {
        var selectedItems = condition.WiredData?.SelectedItems;

        if (selectedItems == null || selectedItems.Count == 0)
        {
            return false;
        }

        return selectedItems.All(selected =>
        {
            var roomItem = room.Room.FurnitureItems.FirstOrDefault(x => x.Id == selected.Id);

            return roomItem != null && room.Room.FurnitureItems.Any(other =>
                other.Id != roomItem.Id &&
                other.PositionX == roomItem.PositionX &&
                other.PositionY == roomItem.PositionY &&
                other.PositionZ > roomItem.PositionZ);
        });
    }
}

public class WiredConditionNotFurnitureHasFurnitureStrategy : WiredConditionFurnitureHasFurnitureStrategy
{
    public override string InteractionType => FurnitureItemInteractionType.WiredConditionNotFurnitureHasFurniture;
    protected override bool Negate => true;
}
