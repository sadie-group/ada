using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Game.Rooms;
using Ada.Networking.Writers.Rooms.Bots;

namespace Ada.Networking.Events.Handlers.Rooms.Bots;

[PacketId(EventHandlerId.RoomBotOpenAction)]
public class RoomBotOpenActionEventHandler(IRoomRepository roomRepository) : INetworkPacketEventHandler
{
    public int BotId { get; init; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out var roomUser))
        {
            return;
        }

        if (room.Room.OwnerId != roomUser.Player.Player.Id)
        {
            return;
        }

        if (!room.BotRepository.TryGetById(BotId, out var roomBot) || roomBot == null)
        {
            return;
        }

        var bot = roomBot.Bot;

        await client.WriteToStreamAsync(new RoomBotActionWriter
        {
            BotId = bot.Id,
            Name = bot.Username ?? string.Empty,
            Motto = bot.Motto ?? string.Empty,
            WalkingMode = bot.WalkingMode,
            AutoChat = bot.AutoChat,
            ChatDelaySeconds = bot.ChatDelaySeconds,
            ChatLines = BotChatLines.Split(bot.ChatLines)
        });
    }
}
