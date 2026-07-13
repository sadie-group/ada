using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Dtos;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Rooms.Users.Groups;

namespace Ada.Networking.Events.Handlers.Rooms.Users.Groups;

[PacketId(EventHandlerId.PlayerGroupBadges)]
public class GetRoomUserGroupBadgesEventHandler : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        var badgeData = new List<IGroupBadgeData>(); // TODO: Fetch
        
        await client.WriteToStreamAsync(new RoomUserGroupBadgeDataWriter
        {
            BadgeData = badgeData
        });
    }
}