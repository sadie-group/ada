using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture.Processors;
using Ada.API.Interfaces.Server.Tasks;

namespace Ada.Server.Tasks.Game.Rooms;

public class ProcessRoomFurnitureItemsTask(
    IRoomRepository roomRepository,
    IEnumerable<IRoomFurnitureItemProcessor> processors) : IServerTask
{
    public TimeSpan PeriodicInterval => TimeSpan.FromMilliseconds(1000);
    public long LastExecutedTicks { get; set; }

    private readonly ParallelOptions _parallelOptions = new()
    {
        MaxDegreeOfParallelism = Environment.ProcessorCount
    };

    public async Task ExecuteAsync()
    {
        await Parallel.ForEachAsync(roomRepository.GetAllRooms(), _parallelOptions, BroadcastItemUpdates);
    }

    private async ValueTask BroadcastItemUpdates(IRoomLogic room, CancellationToken ctx)
    {
        if (room.UserRepository.Count == 0)
        {
            return;
        }

        await room.RunLockedAsync(async () =>
        {
            var queued = false;

            foreach (var processor in processors)
            {
                foreach (var writer in await processor.GetUpdatesForRoomAsync(room))
                {
                    room.QueueBroadcast(writer);
                    queued = true;
                }
            }

            if (queued)
            {
                room.FlushQueuedBroadcasts();
            }
        });
    }
}
