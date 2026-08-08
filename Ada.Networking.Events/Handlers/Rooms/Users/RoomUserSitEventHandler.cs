using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Rooms.Users;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Rooms.Users;

[PacketId(EventHandlerId.RoomUserSit)]
public class RoomUserSitEventHandler(IRoomRepository roomRepository) : INetworkPacketEventHandler, ICountsAsRoomActivity
{
    public Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out _, out var roomUser))
        {
            return Task.CompletedTask;
        }
        
        if ((int) roomUser.Direction % 2 != 0)
        {
            return Task.CompletedTask;
        }
        
        roomUser.AddStatus(RoomUserStatus.Sit, 0.5.ToString());
        
        return Task.CompletedTask;
    }
}