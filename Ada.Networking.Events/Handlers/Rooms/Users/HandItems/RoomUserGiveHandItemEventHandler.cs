using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Rooms.Users.HandItems;

namespace Ada.Networking.Events.Handlers.Rooms.Users.HandItems;

[PacketId(EventHandlerId.RoomUserGiveHandItem)]
public class RoomUserGiveHandItemEventHandler : INetworkPacketEventHandler
{
    public required int UserId { get; init; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        var room = client.RoomUser.Room;
        
        if (!room.UserRepository.TryGetById(UserId, out var toUser) || toUser == null)
        {
            return;
        }

        var fromUser = client.RoomUser;
        var handItemId = fromUser.HandItemId;

        await room.BroadcastDataAsync(new RoomUserHandItemWriter
        {
            UserId = fromUser.Player.Player.Id,
            ItemId = 0
        });

        await toUser.NetworkObject.WriteToStreamAsync(new RoomUserReceivedHandItemWriter
        {
            FromId = fromUser.Player.Player.Id,
            HandItemId = handItemId
        });
        
        await room.BroadcastDataAsync(new RoomUserHandItemWriter
        {
            UserId = toUser.Player.Player.Id,
            ItemId = handItemId
        });

        fromUser.HandItemId = handItemId;
        fromUser.HandItemSet = DateTime.Now;
        
        toUser.HandItemId = handItemId;
        toUser.HandItemSet = DateTime.Now;
    }
}