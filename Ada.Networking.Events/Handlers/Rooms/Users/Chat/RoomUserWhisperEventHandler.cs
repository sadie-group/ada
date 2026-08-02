using Ada.API.DTOs.Rooms.Chat;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Game.WordFilter;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Rooms;
using Ada.Core.Enums.Game.WordFilter;
using Ada.Core.Enums.Miscellaneous;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Constants;
using Ada.Networking.Writers.Rooms.Users;
using Ada.Networking.Writers.Rooms.Users.Chat;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Users.Chat;

[PacketId(EventHandlerId.RoomUserWhisper)]
public class RoomUserWhisperEventHandler(
    IRoomRepository roomRepository, 
    ServerRoomConstants roomConstants,
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomHelperService roomHelperService,
    IWordFilterService wordFilterService,
    IRoomFloodProtectionService floodProtectionService)
    : INetworkPacketEventHandler
{
    public required string Data { get; init; }
    public int Bubble { get; init; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out var roomUser))
        {
            return;
        }

        var whisperData = Data.Split(" ");
        var whisperUsername = whisperData.First();
        var whisperMessage = string.Join(" ", whisperData.Skip(1));

        if (!room.UserRepository.TryGetByUsername(whisperUsername, out var targetUser) || targetUser == null)
        {
            return;
        }
        
        if (string.IsNullOrEmpty(whisperMessage) || whisperMessage.Length > roomConstants.MaxChatMessageLength)
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

        var filterResult = wordFilterService.Filter(whisperMessage, WordFilterContext.Whisper);

        if (filterResult.IsBlocked)
        {
            return;
        }

        whisperMessage = filterResult.FilteredText;

        var chatMessage = new RoomChatMessageDto
        {
            RoomId = room.Room.Id,
            PlayerId = roomUser.Player.Player.Id,
            Message = whisperMessage,
            ChatBubbleId = (ChatBubble) Bubble,
            EmotionId = roomHelperService.GetEmotionFromMessage(whisperMessage),
            TypeId = RoomChatMessageType.Whisper,
            CreatedAt = DateTime.Now
        };

        var packetBytes = new RoomUserWhisperWriter
        {
            SenderId = chatMessage.PlayerId,
            Message = chatMessage.Message,
            EmotionId = (int) chatMessage.EmotionId,
            ChatBubbleId = Bubble,
            MessageLength = chatMessage.Message.Length,
            Urls = []
        };
        
        await roomUser.NetworkObject.WriteToStreamAsync(packetBytes);

        if (filterResult.IsShadowBlocked)
        {
            return;
        }

        await targetUser.NetworkObject.WriteToStreamAsync(packetBytes);

        room.Room.ChatMessages.Add(chatMessage);
    }
}
