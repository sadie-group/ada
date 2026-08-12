using System.Diagnostics;

namespace Ada.API.Interfaces.Server.Tasks;

public interface IServerTask
{
    TimeSpan PeriodicInterval { get; }
    long LastExecutedTicks { get; set; }
    
    public bool WaitingToExecute()
    {
        if (LastExecutedTicks == 0)
        {
            return true;
        }
        
        return Stopwatch.GetElapsedTime(LastExecutedTicks) >= PeriodicInterval;
    }

    Task ExecuteAsync();
}