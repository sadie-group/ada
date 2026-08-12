using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Jukebox;

[PacketId(ServerPacketId.JukeboxTrackCode)]
public class JukeboxTrackCodeWriter : AbstractPacketWriter
{
    public required string Code { get; init; }
    public required int Id { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteString(Code);
        writer.WriteInteger(Id);
    }
}
