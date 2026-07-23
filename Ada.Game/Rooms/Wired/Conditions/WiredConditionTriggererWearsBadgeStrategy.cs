using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Services.Wired;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;

namespace Ada.Game.Rooms.Wired.Conditions;

public class WiredConditionTriggererWearsBadgeStrategy : IWiredConditionStrategy
{
    public virtual string InteractionType => FurnitureItemInteractionType.WiredConditionTriggererWearsBadge;

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
        var badgeCode = condition.WiredData?.Message;

        if (userWhoTriggered == null || string.IsNullOrEmpty(badgeCode))
        {
            return false;
        }

        return userWhoTriggered.Player.Player.Badges.Any(x =>
            string.Equals(x.Badge?.Code, badgeCode, StringComparison.OrdinalIgnoreCase));
    }
}

public class WiredConditionNotTriggererWearsBadgeStrategy : WiredConditionTriggererWearsBadgeStrategy
{
    public override string InteractionType => FurnitureItemInteractionType.WiredConditionNotTriggererWearsBadge;
    protected override bool Negate => true;
}
