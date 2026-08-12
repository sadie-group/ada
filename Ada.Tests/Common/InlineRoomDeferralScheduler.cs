using Ada.API.Interfaces.Game.Rooms;

namespace Ada.Tests.Common;

public sealed class InlineRoomDeferralScheduler : IRoomDeferralScheduler
{
    private List<string> Scheduled { get; } = [];

    public void Schedule(IRoomLogic room, TimeSpan delay, Func<Task> action, string description)
    {
        Scheduled.Add(description);

        room.RunLockedAsync(action).GetAwaiter().GetResult();
    }
}
