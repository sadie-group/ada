using Ada.API.DTOs.Rooms.Chat;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Chat.Commands;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Networking.Client;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Enums.Game.Rooms;
using Ada.Core.Enums.Miscellaneous;
using Ada.Db.Models.Constants;
using Ada.Networking.Writers.Rooms.Users;

namespace Ada.Networking.Events.Application;

public static class RoomChatService
{
    public static async Task OnChatMessageAsync(
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
        {
            return;
        }

        if (!RoomContextResolver.TryResolveRoomObjectsForClient(
                roomRepository,
                client,
                out var room,
                out var roomUser))
        {
            return;
        }

        if (!shouting &&
            message.StartsWith(':') &&
            await RoomCommandService.TryExecuteAsync(
                commandRepository,
                message,
                roomUser))
        {
            return;
        }

        var excludedIds = room.UserRepository
            .GetAll()
            .Where(x =>
                x.Player.Player.OutgoingIgnores
                    .Any(i => i.TargetPlayerId == roomUser.Player.Player.Id))
            .Select(x => x.Player.Player.Id)
            .ToList();

        var emotionId = roomHelperService.GetEmotionFromMessage(message);

        if (shouting)
        {
            await room.BroadcastDataAsync(
                new RoomUserShoutWriter
                {
                    SenderId = roomUser.Player.Player.Id,
                    Message = message,
                    EmotionId = (int)emotionId,
                    ChatBubbleId = (int)bubble,
                    Urls = [],
                    MessageLength = message.Length
                },
                excludedIds);
        }
        else
        {
            await room.BroadcastDataAsync(
                new RoomUserChatWriter
                {
                    SenderId = roomUser.Player.Player.Id,
                    Message = message,
                    EmotionId = (int)emotionId,
                    ChatBubbleId = (int)bubble,
                    Urls = [],
                    MessageLength = message.Length
                },
                excludedIds);
        }

        room.Room.ChatMessages.Add(new RoomChatMessageDto
        {
            RoomId = room.Room.Id,
            PlayerId = roomUser.Player.Player.Id,
            Message = message,
            ChatBubbleId = bubble,
            EmotionId = emotionId,
            TypeId = shouting
                ? RoomChatMessageType.Shout
                : RoomChatMessageType.Normal,
            CreatedAt = DateTime.Now
        });

        var triggers = wiredService.GetTriggers(
            FurnitureItemInteractionType.WiredTriggerSaysSomething,
            room.Room.FurnitureItems,
            message);

        foreach (var trigger in triggers)
        {
            await wiredService.RunTriggerForRoomAsync(
                room,
                trigger,
                roomUser);
        }
    }
}
