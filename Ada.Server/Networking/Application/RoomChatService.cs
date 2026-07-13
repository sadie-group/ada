using Ada.API.DTOs.Rooms.Chat;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Chat.Commands;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Networking.Client;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Enums.Game.Rooms;
using Ada.Core.Enums.Miscellaneous;
using Ada.Db.Models.Constants;
using Ada.Networking.Events;
using Ada.Networking.Writers.Rooms.Users;

namespace Ada.Server.Networking.Application;

public static class RoomChatService
{
    public static async Task HandleAsync(
        INetworkClient client,
        string message,
        bool shouting,
        ServerRoomConstants roomConstants,
        IRoomRepository roomRepository,
        IRoomChatCommandRepository commandRepository,
        ChatBubble bubble,
        IRoomWiredService wiredService,
        IRoomHelperService roomHelperService)
    {
        if (string.IsNullOrWhiteSpace(message) ||
            message.Length > roomConstants.MaxChatMessageLength)
            return;

        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out var roomUser))
            return;

        if (!shouting && message.StartsWith(':') &&
            await RoomCommandService.TryExecuteAsync(commandRepository, message, roomUser))
            return;

        var excludedIds = room.UserRepository
            .GetAll()
            .Where(x => x.Player.Player.OutgoingIgnores.Any(i => i.TargetPlayerId == roomUser.Player.Player.Id))
            .Select(x => x.Player.Player.Id)
            .ToList();

        var writer = shouting
            ? new RoomUserShoutWriter
            {
                SenderId = roomUser.Player.Player.Id,
                Message = message,
                EmotionId = (int)roomHelperService.GetEmotionFromMessage(message),
                ChatBubbleId = (int)bubble,
                Urls = [],
                MessageLength = message.Length
            }
            : new RoomUserChatWriter
            {
                SenderId = roomUser.Player.Player.Id,
                Message = message,
                EmotionId = (int)roomHelperService.GetEmotionFromMessage(message),
                ChatBubbleId = (int)bubble,
                Urls = [],
                MessageLength = message.Length
            };

        await room.BroadcastDataAsync(writer, excludedIds);

        room.Room.ChatMessages.Add(new RoomChatMessageDto
        {
            RoomId = room.Room.Id,
            PlayerId = roomUser.Player.Player.Id,
            Message = message,
            ChatBubbleId = bubble,
            EmotionId = roomHelperService.GetEmotionFromMessage(message),
            TypeId = shouting ? RoomChatMessageType.Shout : RoomChatMessageType.Normal,
            CreatedAt = DateTime.Now
        });

        foreach (var trigger in wiredService.GetTriggers(
                     FurnitureItemInteractionType.WiredTriggerSaysSomething,
                     room.Room.FurnitureItems,
                     message))
        {
            await wiredService.RunTriggerForRoomAsync(room, trigger, roomUser);
        }
    }
}
