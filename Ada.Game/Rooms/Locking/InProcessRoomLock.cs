using Ada.API.Interfaces.Game.Rooms;

namespace Ada.Game.Rooms.Locking;

public sealed class InProcessRoomLock : IRoomLock, IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private long _heldSinceTicks;

    public ValueTask AcquireAsync()
    {
        var wait = _semaphore.WaitAsync();

        if (wait.IsCompletedSuccessfully)
        {
            _heldSinceTicks = Environment.TickCount64;
            return ValueTask.CompletedTask;
        }

        return new ValueTask(AwaitThenStampAsync(wait));
    }

    private async Task AwaitThenStampAsync(Task wait)
    {
        await wait;
        _heldSinceTicks = Environment.TickCount64;
    }

    public void Release()
    {
        _heldSinceTicks = 0;
        _semaphore.Release();
    }

    public long HeldForMilliseconds
    {
        get
        {
            var since = _heldSinceTicks;
            return since == 0 ? 0 : Environment.TickCount64 - since;
        }
    }

    public void Dispose() => _semaphore.Dispose();
}
