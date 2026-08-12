using Ada.API.Interfaces.Game.Jukebox;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Jukebox;

namespace Ada.Networking.Events.Handlers.Jukebox;

[PacketId(EventHandlerId.JukeboxRemoveDisc)]
public class JukeboxRemoveDiscEventHandler(
    IRoomRepository roomRepository,
    IRoomJukeboxService jukeboxService) : INetworkPacketEventHandler
{
    public int OrderIndex { get; init; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null || client.RoomUser?.HasRights() != true)
        {
            return;
        }

        var room = roomRepository.TryGetRoomById(player.State.CurrentRoomId);

        if (room == null)
        {
            return;
        }

        if (await jukeboxService.TryRemoveAsync(room.Room.Id, OrderIndex) == null)
        {
            return;
        }

        await room.BroadcastDataAsync(new JukeboxPlaylistWriter
        {
            Capacity = jukeboxService.MaxTracksPerRoom,
            Tracks = await jukeboxService.GetPlaylistAsync(room.Room.Id)
        });
    }
}
