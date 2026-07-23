using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Services.Wired;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Shared.Helpers;

namespace Ada.Game.Rooms.Wired.Conditions;

public class WiredConditionTimeElapsedMoreStrategy(IWiredTimerService timerService) : IWiredConditionStrategy
{
    public virtual string InteractionType => FurnitureItemInteractionType.WiredConditionTimeElapsedMore;

    public bool IsSatisfied(
        IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto condition,
        IRoomUser? userWhoTriggered)
    {
        var parameters = WiredParameterHelpers.Deserialize(condition.WiredData?.IntParameters);
        var thresholdPulses = parameters.Count > 0 ? parameters[0] : 0;
        var threshold = TimeSpan.FromMilliseconds(thresholdPulses * 500);

        var elapsed = timerService.GetElapsed(room.Room.Id);

        return MoreThan ? elapsed >= threshold : elapsed < threshold;
    }

    protected virtual bool MoreThan => true;
}

public class WiredConditionTimeElapsedLessStrategy(IWiredTimerService timerService)
    : WiredConditionTimeElapsedMoreStrategy(timerService)
{
    public override string InteractionType => FurnitureItemInteractionType.WiredConditionTimeElapsedLess;
    protected override bool MoreThan => false;
}
