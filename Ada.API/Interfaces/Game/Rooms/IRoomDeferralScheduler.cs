namespace Ada.API.Interfaces.Game.Rooms;

public interface IRoomDeferralScheduler
{
    void Schedule(IRoomLogic room, TimeSpan delay, Func<Task> action, string description);
}
