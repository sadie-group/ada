using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Services.Wired;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Shared.Helpers;

namespace Ada.Game.Rooms.Wired.Conditions;

public class WiredConditionUserCountStrategy : IWiredConditionStrategy
{
    public virtual string InteractionType => FurnitureItemInteractionType.WiredConditionUserCountInRoom;

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
        var parameters = WiredParameterHelpers.Deserialize(condition.WiredData?.IntParameters);

        var min = parameters.Count > 0 ? parameters[0] : 0;
        var max = parameters.Count > 1 ? parameters[1] : int.MaxValue;

        var count = room.UserRepository.Count;

        return count >= min && count <= max;
    }
}

public class WiredConditionNotUserCountStrategy : WiredConditionUserCountStrategy
{
    public override string InteractionType => FurnitureItemInteractionType.WiredConditionNotUserCountInRoom;
    protected override bool Negate => true;
}
