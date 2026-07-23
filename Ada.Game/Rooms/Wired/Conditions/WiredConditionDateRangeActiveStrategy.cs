using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Services.Wired;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Shared.Helpers;

namespace Ada.Game.Rooms.Wired.Conditions;

public class WiredConditionDateRangeActiveStrategy : IWiredConditionStrategy
{
    public string InteractionType => FurnitureItemInteractionType.WiredConditionDateRangeActive;

    public bool IsSatisfied(
        IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto condition,
        IRoomUser? userWhoTriggered)
    {
        var parameters = WiredParameterHelpers.Deserialize(condition.WiredData?.IntParameters);

        if (parameters.Count < 2)
        {
            return false;
        }

        var now = DateTimeOffset.Now.ToUnixTimeSeconds();

        return now >= parameters[0] && now <= parameters[1];
    }
}
