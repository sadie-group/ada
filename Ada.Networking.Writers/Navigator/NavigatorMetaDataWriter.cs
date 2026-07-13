using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Navigator;

[PacketId(ServerPacketId.NavigatorMetaData)]
public class NavigatorMetaDataWriter : AbstractPacketWriter
{
    public required Dictionary<string, int> MetaData { get; init; }
}