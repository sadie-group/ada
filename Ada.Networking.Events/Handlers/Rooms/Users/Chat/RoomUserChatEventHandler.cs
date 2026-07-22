using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Chat.Commands;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Game.WordFilter;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Miscellaneous;
using Ada.Core.Shared.Attributes;
using Ada.Db.Models.Constants;
using Ada.Networking.Events.Application;

namespace Ada.Networking.Events.Handlers.Rooms.Users.Chat;

[PacketId(EventHandlerId.RoomUserChat)]
public class RoomUserChatEventHandler(
    IRoomRepository roomRepository, 
    ServerRoomConstants roomConstants, 
    IRoomChatCommandRepository commandRepository,
    IRoomWiredService wiredService,
    IRoomHelperService roomHelperService,
    IWordFilterService wordFilterService,
    IRoomFloodProtectionService floodProtectionService)
    : INetworkPacketEventHandler
{
    public required string Message { get; init; }
    public int Bubble { get; init; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        await RoomChatService.OnChatMessageAsync(client,
            Message,
            false,
            roomConstants,
            roomRepository,
            commandRepository,
            (ChatBubble) Bubble,
            wiredService,
            roomHelperService,
            wordFilterService,
            floodProtectionService);
    }
}