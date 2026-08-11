using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Services.Wired;
using Ada.API.Interfaces.Server.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Ada.Server.Tasks.Game.Rooms
{
    public class DisposeStaleRoomsTask(IRoomRepository roomRepository,
        IWiredTimerService wiredTimerService,
        ILogger<DisposeStaleRoomsTask> logger,
        IConfiguration configuration) : IServerTask
    {
        public TimeSpan PeriodicInterval => TimeSpan.FromSeconds(10);
        public long LastExecutedTicks { get; set; }

        private readonly SemaphoreSlim _semaphore = new(5);

        public async Task ExecuteAsync()
        {
            var keepAlive = configuration.GetValue("RoomOptions:KeepAliveSeconds", 60);

            var staleRooms = roomRepository
                .GetAllRooms()
                .Where(x =>
                    x.UserRepository.NoUsersSince != null &&
                    (DateTime.UtcNow - x.UserRepository.NoUsersSince.Value).TotalSeconds >= keepAlive)
                .OrderBy(x => x.UserRepository.NoUsersSince)
                .Take(100)
                .ToList();

            var tasks = new List<Task>();

            foreach (var room in staleRooms)
            {
                await _semaphore.WaitAsync();

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await room.RunLockedAsync(async () =>
                        {
                            if (room.UserRepository.Count > 0)
                            {
                                return;
                            }

                            roomRepository.TryRemove(room.Room.Id, out _);

                            await room.DisposeAsync();
                        });
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);

            var released = wiredTimerService.RetainOnly(
                roomRepository.GetAllRooms().Select(x => (long) x.Room.Id).ToHashSet());

            if (released > 0)
            {
                logger.LogDebug("Released wired timer state for {Count} unloaded room(s)", released);
            }
        }
    }
}
