using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Game.Rooms;
using Ada.Networking.Events.Attributes;

namespace Ada.Networking.Events.Handlers.Rooms.Moderation;

[PacketId(EventHandlerId.RoomUserKick)]
public class RoomUserKickEventHandler(IRoomRepository roomRepository) : INetworkPacketEventHandler
{
    public int UserId { get; init; }

    [RequiresRoomRights]
    public async Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out var actor))
        {
            return;
        }

        if (!RoomModerationRules.CanActOn(room, actor, UserId, out _))
        {
            return;
        }

        await room.UserRepository.TryRemoveAsync(UserId, notifyLeft: true, hotelView: true);
    }
}
