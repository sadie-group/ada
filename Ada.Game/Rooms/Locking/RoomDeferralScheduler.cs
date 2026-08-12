using Ada.API.Interfaces.Game.Rooms;
using Ada.Core.Shared.Extensions;
using Microsoft.Extensions.Logging;

namespace Ada.Game.Rooms.Locking;

public sealed class RoomDeferralScheduler(ILogger<RoomDeferralScheduler> logger) : IRoomDeferralScheduler
{
    public void Schedule(IRoomLogic room, TimeSpan delay, Func<Task> action, string description)
    {
        using (ExecutionContext.SuppressFlow())
        {
            Task.Run(async () =>
            {
                await Task.Delay(delay);
                await room.RunLockedAsync(action);
            }).FireAndForget(logger, description);
        }
    }
}
