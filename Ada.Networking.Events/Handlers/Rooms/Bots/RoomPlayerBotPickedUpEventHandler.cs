using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Game.Rooms;
using Ada.Networking.Writers.Players.Inventory;
using Ada.Networking.Writers.Rooms.Bots;
using Ada.Networking.Writers.Rooms.Users;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Bots;

[PacketId(EventHandlerId.RoomPlayerBotPickedUp)]
public class RoomPlayerBotPickedUpEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository) : INetworkPacketEventHandler, IDefersPersistence
{
    public required int Id { get; init; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository,
                client,
                out var room,
                out var roomUser))
        {
            return;
        }

        if (room.Room.OwnerId != roomUser.Player.Player.Id)
        {
            await client.WriteToStreamAsync(new RoomBotErrorWriter
            {
                ErrorCode = 4
            });

            return;
        }

        if (!room.BotRepository.TryRemove(Id, out var roomBot) || roomBot == null)
        {
            return;
        }

        foreach (var occupants in room.TileMap.UnitMap.Values)
        {
            occupants.Remove(roomBot);
        }

        await room.BroadcastDataAsync(new RoomUserLeftWriter
        {
            UserId = (room.Room.MaxUsersAllowed + roomBot.Bot.Id).ToString()
        });

        var bot = roomBot.Bot;

        bot.RoomId = 0;

        client.Player!.Player.Bots.Add(bot);

        await client.WriteToStreamAsync(new PlayerInventoryAddBotWriter
        {
            Id = bot.Id,
            Username = bot.Username ?? string.Empty,
            Motto = bot.Motto ?? string.Empty,
            Gender = bot.Gender.ToString(),
            FigureCode = bot.FigureCode ?? string.Empty,
            OpenInventory = true
        });

        var botId = bot.Id;

        _persist = async () =>
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            await dbContext.PlayerBots
                .Where(x => x.Id == botId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.RoomId, 0));
        };
    }

    private Func<Task>? _persist;

    public Task PersistAsync() => _persist?.Invoke() ?? Task.CompletedTask;
}
