using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Events.Dtos;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Users.Groups;

[PacketId(ServerPacketId.RoomUserGroupBadgeData)]
public class RoomUserGroupBadgeDataWriter : AbstractPacketWriter
{
    public required List<IGroupBadgeData> BadgeData { get; set; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(BadgeData.Count);

        foreach (var badge in BadgeData)
        {
            writer.WriteInteger(badge.GroupId);
            writer.WriteString(badge.Badge ?? "");
        }
    }
}
