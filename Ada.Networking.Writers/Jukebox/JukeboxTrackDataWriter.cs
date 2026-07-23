using Ada.API;
using Ada.API.DTOs;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Jukebox;

[PacketId(ServerPacketId.JukeboxTrackData)]
public class JukeboxTrackDataWriter : AbstractPacketWriter
{
    public required IReadOnlyList<SoundTrackDto> Tracks { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(Tracks.Count);

        foreach (var track in Tracks)
        {
            writer.WriteInteger(track.Id);
            writer.WriteString(track.Code);
            writer.WriteString(track.Name);
            writer.WriteString(track.Data);
            writer.WriteInteger(track.Length * 1000);
            writer.WriteString(track.Author);
        }
    }
}
