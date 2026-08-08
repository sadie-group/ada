using System.Drawing;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Rooms.Users;

[PacketId(EventHandlerId.RoomUserWalk)]
public class RoomUserWalkEventHandler(IRoomRepository roomRepository) : INetworkPacketEventHandler, ICountsAsRoomActivity
{
    public int X { get; init; }
    public int Y { get; init; }
    
    public Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out _, out var roomUser))
        {
            return Task.CompletedTask;
        }

        if (!roomUser.CanWalk)
        {
            return Task.CompletedTask;
        }
        
        roomUser.WalkToPoint(new Point(X, Y));
        
        return Task.CompletedTask;
    }
}