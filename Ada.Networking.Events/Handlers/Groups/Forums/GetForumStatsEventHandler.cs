using Ada.API.Interfaces.Game.Groups;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Groups;

namespace Ada.Networking.Events.Handlers.Groups.Forums;

[PacketId(EventHandlerId.GetForumStats)]
public class GetForumStatsEventHandler(
    IGroupRepository groupRepository,
    IGroupForumRepository forumRepository)
    : INetworkPacketEventHandler
{
    public int GuildId { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        var group = await groupRepository.GetByIdAsync(GuildId);
        var stats = await forumRepository.GetStatsAsync(GuildId);

        if (group == null || stats == null)
        {
            return;
        }

        var membership = await groupRepository.GetMembershipAsync(GuildId, player.Player.Id);
        var level = ForumPermissions.LevelFor(group, membership, player.Player.Id);
        var perms = ForumPermissions.Evaluate(group, level);

        await client.WriteToStreamAsync(new GuildForumDataWriter
        {
            Stats = stats,
            ReadPermission = (int) group.ForumReadPermission,
            PostMessagesPermission = (int) group.ForumPostMessagesPermission,
            PostThreadsPermission = (int) group.ForumPostThreadsPermission,
            ModPermission = (int) group.ForumModPermission,
            ErrorRead = perms.ErrorRead,
            ErrorPost = perms.ErrorPost,
            ErrorStartThread = perms.ErrorStartThread,
            ErrorModerate = perms.ErrorModerate,
            CanChangeSettings = perms.CanChangeSettings,
            CanModerate = perms.CanModerate
        });
    }
}
