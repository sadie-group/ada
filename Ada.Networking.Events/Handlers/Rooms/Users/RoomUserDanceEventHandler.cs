using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Game.Rooms;
using Ada.Networking.Writers.Rooms.Users;

namespace Ada.Networking.Events.Handlers.Rooms.Users;

[PacketId(EventHandlerId.RoomUserDance)]
public class RoomUserDanceEventHandler(IRoomRepository roomRepository) : INetworkPacketEventHandler, ICountsAsRoomActivity
{
    public int DanceId { get; init; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out var roomUser))
        {
            return;
        }
        
        await room.BroadcastDataAsync(new RoomUserDanceWriter
        {
            UserId = roomUser.Player.Player.Id,
            DanceId = DanceId
        });
    }
}