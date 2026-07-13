using System.Diagnostics;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Server.Tasks;

namespace Ada.Server.Tasks.Other;

public class UpdateConsoleTitleTask(
    IPlayerRepository playerRepository, 
    IRoomRepository roomRepository) : IServerTask
{
    public TimeSpan PeriodicInterval => TimeSpan.FromSeconds(1);
    public long LastExecutedTicks { get; set; }

    public Task ExecuteAsync()
    {
        var usersOnline = playerRepository.Count();
        var roomCount = roomRepository.Count;
        var started = Process.GetCurrentProcess().StartTime;
        var assembly = typeof(Server).Assembly;
        var version = assembly.GetName().Version;
        
        Console.Title = $"Ada {version} - Started: {started:HH:mm:ss} - Players: {usersOnline} - Rooms: {roomCount}";
        return Task.CompletedTask;
    }
}