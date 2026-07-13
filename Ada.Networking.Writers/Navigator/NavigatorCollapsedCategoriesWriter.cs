using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Navigator;

[PacketId(ServerPacketId.NavigatorCollapsedCategories)]
public class NavigatorCollapsedCategoriesWriter : AbstractPacketWriter
{
    public required List<string> Categories { get; init; }
}