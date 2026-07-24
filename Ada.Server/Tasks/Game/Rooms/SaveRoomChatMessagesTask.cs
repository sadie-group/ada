using Ada.API.DTOs.Rooms.Chat;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Server.Tasks;
using Ada.Db;
using Ada.Db.Models.Rooms.Chat;
using AutoMapper;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace Ada.Server.Tasks.Game.Rooms;

public class SaveRoomChatMessagesTask(IRoomRepository roomRepository,
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IMapper mapper) : IServerTask
{
    public TimeSpan PeriodicInterval => TimeSpan.FromSeconds(10);
    public long LastExecutedTicks { get; set; }

    public async Task ExecuteAsync()
    {
        var messagesToSave = new List<RoomChatMessageDto>();
        
        foreach (var room in roomRepository.GetAllRooms())
        {
            var chatMessages = room
                .Room.ChatMessages
                .Where(x => x.Id == 0)
                .ToList();

            if (chatMessages.Count == 0)
            {
                continue;
            }

            messagesToSave.AddRange(chatMessages);
        }

        var entitiesToSave = mapper.Map<List<RoomChatMessage>>(messagesToSave);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.BulkInsertAsync(entitiesToSave);
    }
}