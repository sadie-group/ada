using Microsoft.EntityFrameworkCore;
using Ada.API.DTOs.Rooms.Chat;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Rooms;
using Ada.Core.Enums.Miscellaneous;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Constants;
using Ada.Networking.Writers.Rooms.Users;

namespace Ada.Networking.Events.Handlers.Rooms.Users.Chat;

[PacketId(EventHandlerId.RoomUserWhisper)]
public class RoomUserWhisperEventHandler(
    IRoomRepository roomRepository, 
    ServerRoomConstants roomConstants,
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomHelperService roomHelperService)
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
        await targetUser.NetworkObject.WriteToStreamAsync(packetBytes);
        
        room.Room.ChatMessages.Add(chatMessage);
    }
}
