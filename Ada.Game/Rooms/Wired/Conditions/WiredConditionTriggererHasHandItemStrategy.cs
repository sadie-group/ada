using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Services.Wired;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Shared.Helpers;

namespace Ada.Game.Rooms.Wired.Conditions;

public class WiredConditionTriggererHasHandItemStrategy : IWiredConditionStrategy
{
    public string InteractionType => FurnitureItemInteractionType.WiredConditionTriggererHasHandItem;

    public bool IsSatisfied(
        IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto condition,
        IRoomUser? userWhoTriggered)
    {
        if (userWhoTriggered == null)
        {
            return false;
        }

        var parameters = WiredParameterHelpers.Deserialize(condition.WiredData?.IntParameters);
        var handItemId = parameters.Count > 0 ? parameters[0] : 0;

        return handItemId > 0
            ? userWhoTriggered.HandItemId == handItemId
            : userWhoTriggered.HandItemId > 0;
    }
}
