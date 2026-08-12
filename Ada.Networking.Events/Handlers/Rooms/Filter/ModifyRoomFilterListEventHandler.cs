using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Rooms;
using Ada.Networking.Writers.Rooms.Filter;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Filter;

[PacketId(EventHandlerId.ModifyRoomFilterList)]
public class ModifyRoomFilterListEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository,
    IRoomWordFilterService roomWordFilterService) : INetworkPacketEventHandler
{
    public bool Add { get; init; }
    public required string Word { get; init; }

    private const int _maxWordLength = 32;
    private const int _maxWordsPerRoom = 50;

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        var room = roomRepository.TryGetRoomById(player.State.CurrentRoomId);

        if (room == null || room.Room.OwnerId != player.Player.Id)
        {
            return;
        }

        var word = Word.Trim().ToLowerInvariant();

        if (word.Length is 0 or > _maxWordLength)
        {
            return;
        }

        var roomId = room.Room.Id;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        if (Add)
        {
            var count = await dbContext.RoomWordFilters.CountAsync(x => x.RoomId == roomId);

            if (count >= _maxWordsPerRoom ||
                await dbContext.RoomWordFilters.AnyAsync(x => x.RoomId == roomId && x.Word == word))
            {
                return;
            }

            dbContext.RoomWordFilters.Add(new RoomWordFilter
            {
                RoomId = roomId,
                Word = word,
                CreatedAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();
        }
        else
        {
            await dbContext.RoomWordFilters
                .Where(x => x.RoomId == roomId && x.Word == word)
                .ExecuteDeleteAsync();
        }

        roomWordFilterService.Invalidate(roomId);

        var words = await dbContext.RoomWordFilters
            .Where(x => x.RoomId == roomId)
            .OrderBy(x => x.Id)
            .Select(x => x.Word)
            .ToListAsync();

        await client.WriteToStreamAsync(new RoomFilterListWriter
        {
            Words = words
        });
    }
}
