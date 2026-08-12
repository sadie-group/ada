using Ada.API.Interfaces.Game.Jukebox;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Jukebox;

namespace Ada.Networking.Events.Handlers.Jukebox;

[PacketId(EventHandlerId.JukeboxGetPlaylist)]
public class JukeboxGetPlaylistEventHandler(
    IRoomRepository roomRepository,
    IRoomJukeboxService jukeboxService) : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        var room = roomRepository.TryGetRoomById(player.State.CurrentRoomId);

        if (room == null)
        {
            return;
        }

        await client.WriteToStreamAsync(new JukeboxPlaylistWriter
        {
            Capacity = jukeboxService.MaxTracksPerRoom,
            Tracks = await jukeboxService.GetPlaylistAsync(room.Room.Id)
        });
    }
}
