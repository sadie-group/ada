using Ada.API.DTOs.Rooms.Chat;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Chat.Commands;
using Ada.Game.Rooms.Chat.Commands;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Game.WordFilter;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Client;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Enums.Game.Rooms;
using Ada.Core.Enums.Game.Rooms.Users;
using Ada.Core.Enums.Game.WordFilter;
using Ada.Core.Enums.Miscellaneous;
using Ada.Db.Models.Constants;
using Ada.Networking.Writers.Rooms.Users;
using Ada.Networking.Writers.Rooms.Users.Chat;

namespace Ada.Game.Rooms.Chat;

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
        IRoomHelperService roomHelperService,
        IWordFilterService wordFilterService,
        IRoomFloodProtectionService floodProtectionService,
        IRoomWordFilterService roomWordFilterService)
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

        if (room.Room.IsMuted && !roomUser.HasRights())
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

        var playerId = roomUser.Player.Player.Id;

        if (floodProtectionService.IsMuted(playerId, out var remainingSeconds))
        {
            await roomUser.NetworkObject.WriteToStreamAsync(new RoomUserFloodControlWriter
            {
                Seconds = remainingSeconds
            });
            return;
        }

        var muteSeconds = floodProtectionService.RegisterMessage(
            playerId,
            room.Room.ChatSettings?.ChatProtection ?? 0,
            playerId == room.Room.OwnerId);

        if (muteSeconds != null)
        {
            await roomUser.NetworkObject.WriteToStreamAsync(new RoomUserFloodControlWriter
            {
                Seconds = muteSeconds.Value
            });
            return;
        }

        if (await roomWordFilterService.ContainsFilteredWordAsync(room.Room.Id, message))
        {
            return;
        }

        var filterResult = wordFilterService.Filter(message, WordFilterContext.Chat);

        if (filterResult.IsBlocked)
        {
            return;
        }

        message = filterResult.FilteredText;

        var emotionId = roomHelperService.GetEmotionFromMessage(message);

        if (filterResult.IsShadowBlocked)
        {
            await roomUser.NetworkObject.WriteToStreamAsync(BuildChatWriter(
                shouting,
                playerId,
                message,
                emotionId,
                bubble));
            return;
        }

        var excludedIds = ResolveIgnoringPlayerIds(room, playerId);

        await room.BroadcastDataAsync(
            BuildChatWriter(shouting, playerId, message, emotionId, bubble),
            excludedIds);

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

    private static List<long>? ResolveIgnoringPlayerIds(IRoomLogic room, long senderId)
    {
        List<long>? excluded = null;

        foreach (var user in room.UserRepository.GetAll())
        {
            foreach (var ignore in user.Player.Player.OutgoingIgnores)
            {
                if (ignore.TargetPlayerId != senderId)
                {
                    continue;
                }

                (excluded ??= []).Add(user.Player.Player.Id);
                break;
            }
        }

        return excluded;
    }

    private static AbstractPacketWriter BuildChatWriter(
        bool shouting,
        long senderId,
        string message,
        RoomUserEmotion emotionId,
        ChatBubble bubble)
    {
        if (shouting)
        {
            return new RoomUserShoutWriter
            {
                SenderId = senderId,
                Message = message,
                EmotionId = (int)emotionId,
                ChatBubbleId = (int)bubble,
                Urls = [],
                MessageLength = message.Length
            };
        }

        return new RoomUserChatWriter
        {
            SenderId = senderId,
            Message = message,
            EmotionId = (int)emotionId,
            ChatBubbleId = (int)bubble,
            Urls = [],
            MessageLength = message.Length
        };
    }
}
