using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Jukebox;

[PacketId(ServerPacketId.JukeboxDiscs)]
public class JukeboxDiscsWriter : AbstractPacketWriter
{
    public required IReadOnlyList<KeyValuePair<int, int>> Discs { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(Discs.Count);

        foreach (var (itemId, soundTrackId) in Discs)
        {
            writer.WriteInteger(itemId);
            writer.WriteInteger(soundTrackId);
        }
    }
}
