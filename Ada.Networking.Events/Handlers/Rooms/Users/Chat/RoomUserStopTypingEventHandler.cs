using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Rooms.Users.Chat;

namespace Ada.Networking.Events.Handlers.Rooms.Users.Chat;

[PacketId(EventHandlerId.RoomUserStopTyping)]
public class RoomUserStopTypingEventHandler(IRoomRepository roomRepository) : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        var roomUser = client.RoomUser;
        
        if (roomUser == null)
        {
            return;
        }

        await roomUser.Room.BroadcastDataAsync(new RoomUserTypingWriter
        {
            UserId = roomUser.Player.Player.Id,
            IsTyping = false
        });
    }
}