using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Rooms.Users;
using Ada.Core.Shared.Attributes;
using Ada.Game.Rooms;
using Ada.Networking.Writers.Rooms.Users;

namespace Ada.Networking.Events.Handlers.Rooms.Users;

[PacketId(EventHandlerId.RoomUserAction)]
public class RoomUserActionEventHandler(IRoomRepository roomRepository) : INetworkPacketEventHandler, ICountsAsRoomActivity
{
    public int Action { get; init; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out var roomUser))
        {
            return;
        }

        if (Action == (int) RoomUserAction.Idle)
        {
            if (!roomUser.IsIdle)
            {
                roomUser.LastAction -= roomUser.IdleTime;
            }
            
            await room.BroadcastDataAsync(new RoomUserIdleWriter
            {
                UserId = roomUser.Player.Player.Id,
                IsIdle = roomUser.IsIdle
            });
            
            return;
        }

        await room.BroadcastDataAsync(new RoomUserActionWriter
        {
            UserId = roomUser.Player.Player.Id,
            Action = Action
        });
    }
}