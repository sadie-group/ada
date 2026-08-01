using Ada.API.Interfaces.Game.Groups;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Groups;

namespace Ada.Networking.Events.Handlers.Groups.Forums;

[PacketId(EventHandlerId.ForumUpdateThread)]
public class ForumUpdateThreadEventHandler(
    IGroupRepository groupRepository,
    IGroupForumRepository forumRepository)
    : INetworkPacketEventHandler
{
    public int GuildId { get; set; }
    public int ThreadId { get; set; }
    public bool IsPinned { get; set; }
    public bool IsLocked { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null || !await CanModerateAsync(player.Player.Id))
        {
            return;
        }

        await forumRepository.SetThreadPinnedLockedAsync(ThreadId, IsPinned, IsLocked);

        var thread = await forumRepository.GetThreadAsync(GuildId, ThreadId);

        if (thread != null)
        {
            await client.WriteToStreamAsync(new GuildForumSingleThreadUpdatedWriter
            {
                GuildId = GuildId,
                Thread = thread
            });
        }
    }

    private async Task<bool> CanModerateAsync(long playerId)
    {
        var group = await groupRepository.GetByIdAsync(GuildId);

        if (group == null)
        {
            return false;
        }

        var membership = await groupRepository.GetMembershipAsync(GuildId, playerId);
        var level = ForumPermissions.LevelFor(group, membership, playerId);
        return ForumPermissions.Evaluate(group, level).CanModerate;
    }
}
