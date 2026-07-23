using Ada.API.Interfaces.Game.Groups;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Groups;

namespace Ada.Networking.Events.Handlers.Groups;

[PacketId(EventHandlerId.RequestGuildMembers)]
public class RequestGuildMembersEventHandler(IGroupRepository groupRepository)
    : INetworkPacketEventHandler
{
    public int GroupId { get; set; }
    public int PageId { get; set; }
    public string Query { get; set; } = "";
    public int LevelId { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        var group = await groupRepository.GetByIdAsync(GroupId);

        if (group == null)
        {
            return;
        }

        var viewerIsAdmin = await groupRepository.HasAdminRightsAsync(group, player.Player.Id);

        var (members, total) = await groupRepository.GetMembersAsync(
            group.Id, PageId, Query ?? "", LevelId, GuildMembersWriter.MembersPerPage);

        await client.WriteToStreamAsync(new GuildMembersWriter
        {
            GuildId = group.Id,
            GuildName = group.Name,
            RoomId = group.RoomId,
            Badge = group.Badge,
            OwnerId = group.PlayerId,
            TotalMemberCount = total,
            Members = members,
            ViewerIsAdmin = viewerIsAdmin,
            PageId = PageId,
            LevelId = LevelId,
            SearchValue = Query ?? ""
        });
    }
}
