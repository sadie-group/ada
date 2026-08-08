using Ada.API.Interfaces.Game.Rooms;

namespace Ada.Game.Rooms.Locking;

public sealed class InProcessRoomLock : IRoomLock, IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public ValueTask AcquireAsync()
    {
        var wait = _semaphore.WaitAsync();

        return wait.IsCompletedSuccessfully ? ValueTask.CompletedTask : new ValueTask(wait);
    }

    public void Release() => _semaphore.Release();

    public void Dispose() => _semaphore.Dispose();
}
