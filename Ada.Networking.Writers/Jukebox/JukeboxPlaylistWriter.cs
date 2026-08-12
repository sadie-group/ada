using Ada.API;
using Ada.API.DTOs.Jukebox;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Jukebox;

[PacketId(ServerPacketId.JukeboxPlaylist)]
public class JukeboxPlaylistWriter : AbstractPacketWriter
{
    public required int Capacity { get; init; }
    public required IReadOnlyList<RoomJukeboxTrackDto> Tracks { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(Capacity);
        writer.WriteInteger(Tracks.Count);
        writer.WriteInteger(Tracks.Count);

        foreach (var track in Tracks)
        {
            writer.WriteInteger(track.OrderIndex);
            writer.WriteInteger(track.SoundTrackId);
        }
    }
}
