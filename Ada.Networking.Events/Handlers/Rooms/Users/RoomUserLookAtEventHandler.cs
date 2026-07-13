using System.Drawing;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Rooms.Users;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Rooms.Users;

[PacketId(EventHandlerId.RoomUserLookAt)]
public class RoomUserLookAtEventHandler(IRoomRepository roomRepository) : INetworkPacketEventHandler
{
    public int X { get; init; }
    public int Y { get; init; }
    
    public Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out _, out var roomUser))
        {
            return Task.CompletedTask;
        }
        
        var currentPoint = roomUser.Point;
        
        if (roomUser.StatusMap.ContainsKey(RoomUserStatus.Lay) || 
            roomUser.IsWalking ||
            currentPoint.X == X && currentPoint.Y == Y)
        {
            return Task.CompletedTask;
        }

        roomUser.LookAtPoint(new Point(X, Y));
        return Task.CompletedTask;
    }
}