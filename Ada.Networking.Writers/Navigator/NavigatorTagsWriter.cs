using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Navigator;

[PacketId(ServerPacketId.NavigatorTags)]
public class NavigatorTagsWriter : AbstractPacketWriter
{
    public required Dictionary<string, int> Tags { get; init; }
}
