using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Game.Rooms;
using Ada.Networking.Writers.Rooms.Users.Chat;

namespace Ada.Networking.Events.Handlers.Rooms.Users.Chat;

[PacketId(EventHandlerId.RoomUserStartTyping)]
public class RoomUserStartTypingEventHandler(IRoomRepository roomRepository) : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out var roomUser))
        {
            return;
        }

        await room.BroadcastDataAsync(new RoomUserTypingWriter
        {
            UserId = roomUser.Player.Player.Id,
            IsTyping = true
        });
    }
}