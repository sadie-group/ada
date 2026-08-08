using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Rooms.Users;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Rooms.Users;

[PacketId(EventHandlerId.RoomUserSign)]
public class RoomUserSignEventHandler(IRoomRepository roomRepository) : INetworkPacketEventHandler, ICountsAsRoomActivity
{
    public int SignId { get; init; }
    
    public Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out _, out var roomUser))
        {
            return Task.CompletedTask;
        }

        roomUser.AddStatus(RoomUserStatus.Sign, SignId.ToString());
        roomUser.SignSet = DateTime.Now;
        
        return Task.CompletedTask;
    }
}