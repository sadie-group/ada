using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Navigator;

[PacketId(ServerPacketId.NavigatorEventCategories)]
public class NavigatorEventCategoriesWriter : AbstractPacketWriter
{
    public required List<RoomCategoryDto> Categories { get; init; }
}