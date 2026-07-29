using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Rooms;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Navigator;

namespace Ada.Networking.Events.Handlers.Navigator;

[PacketId(EventHandlerId.NavigatorPromotedRooms)]
public class NavigatorPromotedRoomsEventHandler : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        await client.WriteToStreamAsync(new NavigatorGuestRoomSearchResultWriter
        {
            SearchType = 2,
            SearchParam = "",
            Rooms =
            [
            ],
            HasAdditional = true,
            OfficialRoomEntryData = new OfficialRoomEntryData
            {
                Index = 0,
                PopupCaption = "A",
                PopupDescription = "B",
                ShowDetails = 1,
                PictureText = "C",
                PictureRef = "D",
                FolderId = 1,
                UserCount = 1,
                Type = OfficialRoomEntryDataType.Tag,
                Unknown14 = "E"
            }
        });
    }
}