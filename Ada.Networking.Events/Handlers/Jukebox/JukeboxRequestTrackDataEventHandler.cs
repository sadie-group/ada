using Ada.API.Interfaces.Game.Jukebox;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Jukebox;

namespace Ada.Networking.Events.Handlers.Jukebox;

[PacketId(EventHandlerId.JukeboxRequestTrackData)]
public class JukeboxRequestTrackDataEventHandler(ISoundTrackRepository soundTrackRepository)
    : INetworkPacketEventHandler
{
    public List<int> SongIds { get; set; } = [];

    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null)
        {
            return;
        }

        var tracks = await soundTrackRepository.GetByIdsAsync(SongIds);

        await client.WriteToStreamAsync(new JukeboxTrackDataWriter
        {
            Tracks = tracks
        });
    }
}
