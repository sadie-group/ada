using System.Collections.Concurrent;
using Ada.API.DTOs.Rooms.Chat;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Server.Tasks;
using Ada.Db;
using Ada.Db.Models.Rooms.Chat;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Server.Tasks.Game.Rooms;

public class SaveRoomChatMessagesTask(IRoomRepository roomRepository,
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IMapper mapper) : IServerTask
{
    public TimeSpan PeriodicInterval => TimeSpan.FromSeconds(10);
    public long LastExecutedTicks { get; set; }

    private const int _maxRetainedMessagesPerRoom = 150;

    private readonly ParallelOptions _parallelOptions = new()
    {
        MaxDegreeOfParallelism = Environment.ProcessorCount
    };

    public async Task ExecuteAsync()
    {
        var collected = new ConcurrentBag<List<RoomChatMessageDto>>();

        await Parallel.ForEachAsync(
            roomRepository.GetAllRooms(),
            _parallelOptions,
            async (room, _) =>
            {
                await room.RunLockedAsync(() =>
                {
                    var pending = room.Room.ChatMessages.Where(x => x.Id == 0).ToList();

                    if (pending.Count > 0)
                    {
                        collected.Add(pending);
                    }

                    TrimPersistedMessages(room.Room.ChatMessages);

                    return Task.CompletedTask;
                });
            });

        var messagesToSave = collected.SelectMany(x => x).ToList();

        if (messagesToSave.Count == 0)
        {
            return;
        }

        var entitiesToSave = mapper.Map<List<RoomChatMessage>>(messagesToSave);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        dbContext.RoomChatMessages.AddRange(entitiesToSave);
        await dbContext.SaveChangesAsync();

        for (var i = 0; i < messagesToSave.Count; i++)
        {
            messagesToSave[i].Id = entitiesToSave[i].Id;
        }
    }

    private static void TrimPersistedMessages(ICollection<RoomChatMessageDto> chatMessages)
    {
        var persistedOverflow = chatMessages.Count(x => x.Id != 0) - _maxRetainedMessagesPerRoom;

        if (persistedOverflow <= 0)
        {
            return;
        }

        foreach (var message in chatMessages.Where(x => x.Id != 0).Take(persistedOverflow).ToList())
        {
            chatMessages.Remove(message);
        }
    }
}
