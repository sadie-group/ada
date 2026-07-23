using Ada.API.Interfaces.Game.Jukebox;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Jukebox;

namespace Ada.Networking.Events.Handlers.Jukebox;

[PacketId(EventHandlerId.JukeboxRequestTrackCode)]
public class JukeboxRequestTrackCodeEventHandler(ISoundTrackRepository soundTrackRepository)
    : INetworkPacketEventHandler
{
    public string SongName { get; set; } = "";

    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null)
        {
            return;
        }

        var track = await soundTrackRepository.GetByNameAsync(SongName);

        if (track == null)
        {
            return;
        }

        await client.WriteToStreamAsync(new JukeboxTrackCodeWriter
        {
            Code = track.Code,
            Id = track.Id
        });
    }
}
