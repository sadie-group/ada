using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Groups;

[PacketId(ServerPacketId.GuildEditFail)]
public class GuildEditFailWriter : AbstractPacketWriter
{
    public required int ErrorCode { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(ErrorCode);
    }
}
