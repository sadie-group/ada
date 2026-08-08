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

    public async Task ExecuteAsync()
    {
        var messagesToSave = new List<RoomChatMessageDto>();

        foreach (var room in roomRepository.GetAllRooms())
        {
            await room.RunLockedAsync(() =>
            {
                messagesToSave.AddRange(room.Room.ChatMessages.Where(x => x.Id == 0));

                TrimPersistedMessages(room.Room.ChatMessages);

                return Task.CompletedTask;
            });
        }

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
