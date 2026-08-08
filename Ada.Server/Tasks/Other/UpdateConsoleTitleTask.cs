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

    private static readonly string _titlePrefix = BuildTitlePrefix();

    private static string BuildTitlePrefix()
    {
        using var process = Process.GetCurrentProcess();

        var version = typeof(Server).Assembly.GetName().Version;

        return $"Ada {version} - Started: {process.StartTime:HH:mm:ss}";
    }

    public Task ExecuteAsync()
    {
        var usersOnline = playerRepository.Count();
        var roomCount = roomRepository.Count;

        Console.Title = $"{_titlePrefix} - Players: {usersOnline} - Rooms: {roomCount}";
        return Task.CompletedTask;
    }
}
