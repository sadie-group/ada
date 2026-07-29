using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture.Processors;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Server.Tasks;

namespace Ada.Server.Tasks.Game.Rooms;

public class ProcessRoomFurnitureItemsTask(
    IRoomRepository roomRepository, 
    IEnumerable<IRoomFurnitureItemProcessor> processors) : IServerTask
{
    public TimeSpan PeriodicInterval => TimeSpan.FromMilliseconds(1000);
    public long LastExecutedTicks { get; set; }
    
    public async Task ExecuteAsync()
    {
        await Parallel.ForEachAsync(roomRepository.GetAllRooms(), BroadcastItemUpdates);
    }

    private async ValueTask BroadcastItemUpdates(IRoomLogic room, CancellationToken ctx)
    {
        if (room.UserRepository.Count == 0)
        {
            return;
        }

        await room.RunLockedAsync(async () =>
        {
            var writersToBroadcast = await GetItemUpdatesAsync(room);

            foreach (var writer in writersToBroadcast)
            {
                await room.BroadcastDataAsync(writer);
            }
        });
    }

    private async Task<IEnumerable<AbstractPacketWriter>> GetItemUpdatesAsync(IRoomLogic room)
    {
        var writers = new List<AbstractPacketWriter>();

        foreach (var processor in processors)
        {
            writers.AddRange(await processor.GetUpdatesForRoomAsync(room));
        }
        
        return writers;
    }
}