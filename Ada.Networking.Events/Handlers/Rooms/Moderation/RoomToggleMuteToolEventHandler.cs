using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Rooms.Moderation;

[PacketId(EventHandlerId.RoomToggleMuteTool)]
public class RoomToggleMuteToolEventHandler(IRoomRepository roomRepository) : INetworkPacketEventHandler
{
    public Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null || client.RoomUser?.HasRights() != true)
        {
            return Task.CompletedTask;
        }

        var room = roomRepository.TryGetRoomById(player.State.CurrentRoomId);

        room?.Room.IsMuted = !room.Room.IsMuted;

        return Task.CompletedTask;
    }
}
