using Ada.API.Interfaces.Game.Groups;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Groups;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Events.Handlers.Groups;
using Ada.Networking.Writers.Groups;

namespace Ada.Networking.Events.Handlers.Groups.Forums;

[PacketId(EventHandlerId.UpdateForumSettings)]
public class UpdateForumSettingsEventHandler(
    IGroupRepository groupRepository,
    IGroupForumRepository forumRepository)
    : INetworkPacketEventHandler
{
    public int GuildId { get; set; }
    public int CanRead { get; set; }
    public int PostMessages { get; set; }
    public int PostThreads { get; set; }
    public int ModForum { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        var group = await groupRepository.GetByIdAsync(GuildId);

        if (group == null || !groupRepository.IsOwner(group, player.Player.Id))
        {
            return;
        }

        await forumRepository.UpdateForumSettingsAsync(
            GuildId, Level(CanRead), Level(PostMessages), Level(PostThreads), Level(ModForum));

        var stats = await forumRepository.GetStatsAsync(GuildId);
        var updated = await groupRepository.GetByIdAsync(GuildId);

        if (stats == null || updated == null)
        {
            return;
        }

        var perms = ForumPermissions.Evaluate(updated, 3);

        await client.WriteToStreamAsync(new GuildForumDataWriter
        {
            Stats = stats,
            ReadPermission = (int) updated.ForumReadPermission,
            PostMessagesPermission = (int) updated.ForumPostMessagesPermission,
            PostThreadsPermission = (int) updated.ForumPostThreadsPermission,
            ModPermission = (int) updated.ForumModPermission,
            ErrorRead = perms.ErrorRead,
            ErrorPost = perms.ErrorPost,
            ErrorStartThread = perms.ErrorStartThread,
            ErrorModerate = perms.ErrorModerate,
            CanChangeSettings = perms.CanChangeSettings,
            CanModerate = perms.CanModerate
        });
    }

    private static ForumPermissionLevel Level(int value)
        => Enum.IsDefined(typeof(ForumPermissionLevel), value)
            ? (ForumPermissionLevel) value
            : ForumPermissionLevel.Members;
}
