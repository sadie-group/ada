using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Services.Wired;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;

namespace Ada.Game.Rooms.Wired.Conditions;

public class WiredConditionFurnitureHasUsersStrategy : IWiredConditionStrategy
{
    public virtual string InteractionType => FurnitureItemInteractionType.WiredConditionFurnitureHasUsers;

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

        var users = room.UserRepository.GetAll();

        return selectedItems.All(selected =>
            room.Room.FurnitureItems.Any(item =>
                item.Id == selected.Id &&
                users.Any(user =>
                    user.Point.X == item.PositionX &&
                    user.Point.Y == item.PositionY)));
    }
}

public class WiredConditionNotFurnitureHasUsersStrategy : WiredConditionFurnitureHasUsersStrategy
{
    public override string InteractionType => FurnitureItemInteractionType.WiredConditionNotFurnitureHasUsers;
    protected override bool Negate => true;
}
