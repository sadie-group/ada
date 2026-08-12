using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Game.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Bots;

[PacketId(EventHandlerId.RoomBotSaveAction)]
public class RoomBotSaveActionEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository) : INetworkPacketEventHandler, IDefersPersistence
{
    public int BotId { get; init; }
    public int WalkingMode { get; init; }
    public bool AutoChat { get; init; }
    public int ChatDelaySeconds { get; init; }
    public required List<string> ChatLines { get; init; }

    private const int _maxWalkingMode = 2;

    public Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out var roomUser))
        {
            return Task.CompletedTask;
        }

        if (room.Room.OwnerId != roomUser.Player.Player.Id)
        {
            return Task.CompletedTask;
        }

        if (!room.BotRepository.TryGetById(BotId, out var roomBot) || roomBot == null)
        {
            return Task.CompletedTask;
        }

        var bot = roomBot.Bot;

        bot.WalkingMode = Math.Clamp(WalkingMode, 0, _maxWalkingMode);
        bot.AutoChat = AutoChat;
        bot.ChatDelaySeconds = Math.Clamp(
            ChatDelaySeconds,
            BotChatLines.MinChatDelaySeconds,
            BotChatLines.MaxChatDelaySeconds);
        bot.ChatLines = BotChatLines.Join(ChatLines);

        var botId = bot.Id;
        var walkingMode = bot.WalkingMode;
        var autoChat = bot.AutoChat;
        var delay = bot.ChatDelaySeconds;
        var lines = bot.ChatLines;

        _persist = async () =>
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            await dbContext.PlayerBots
                .Where(x => x.Id == botId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.WalkingMode, walkingMode)
                    .SetProperty(x => x.AutoChat, autoChat)
                    .SetProperty(x => x.ChatDelaySeconds, delay)
                    .SetProperty(x => x.ChatLines, lines));
        };

        return Task.CompletedTask;
    }

    private Func<Task>? _persist;

    public Task PersistAsync() => _persist?.Invoke() ?? Task.CompletedTask;
}
