using Microsoft.Extensions.Logging;

namespace Ada.Core.Shared.Extensions;

public static class TaskExtensions
{
    public static void FireAndForget(this Task task, ILogger logger, string context)
    {
        task.ContinueWith(
            t => logger.LogError(t.Exception, "Background task failed: {Context}", context),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }
}
