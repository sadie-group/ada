using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Services.Wired;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Shared.Helpers;

namespace Ada.Game.Rooms.Wired.Conditions;

public class WiredConditionTriggererWearsEffectStrategy : IWiredConditionStrategy
{
    public virtual string InteractionType => FurnitureItemInteractionType.WiredConditionTriggererWearsEffect;

    public bool IsSatisfied(
        IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto condition,
        IRoomUser? userWhoTriggered)
    {
        return Evaluate(condition, userWhoTriggered) != Negate;
    }

    protected virtual bool Negate => false;

    private static bool Evaluate(PlayerFurnitureItemPlacementDataDto condition, IRoomUser? userWhoTriggered)
    {
        if (userWhoTriggered == null)
        {
            return false;
        }

        var parameters = WiredParameterHelpers.Deserialize(condition.WiredData?.IntParameters);
        var effectId = parameters.Count > 0 ? parameters[0] : 0;

        return effectId > 0
            ? userWhoTriggered.ActiveEffectId == effectId
            : userWhoTriggered.ActiveEffectId > 0;
    }
}

public class WiredConditionNotTriggererWearsEffectStrategy : WiredConditionTriggererWearsEffectStrategy
{
    public override string InteractionType => FurnitureItemInteractionType.WiredConditionNotTriggererWearsEffect;
    protected override bool Negate => true;
}
