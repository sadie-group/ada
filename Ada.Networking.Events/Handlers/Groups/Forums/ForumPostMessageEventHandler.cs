using Ada.API.Interfaces.Game.Groups;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Events.Handlers.Groups;
using Ada.Networking.Writers.Groups;

namespace Ada.Networking.Events.Handlers.Groups.Forums;

[PacketId(EventHandlerId.ForumPostMessage)]
public class ForumPostMessageEventHandler(
    IGroupRepository groupRepository,
    IGroupForumRepository forumRepository)
    : INetworkPacketEventHandler
{
    public int GuildId { get; set; }
    public int ThreadId { get; set; }
    public string Subject { get; set; } = "";
    public string Message { get; set; } = "";

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null || string.IsNullOrWhiteSpace(Message))
        {
            return;
        }

        var group = await groupRepository.GetByIdAsync(GuildId);

        if (group == null)
        {
            return;
        }

        var membership = await groupRepository.GetMembershipAsync(GuildId, player.Player.Id);
        var level = ForumPermissions.LevelFor(group, membership, player.Player.Id);
        var perms = ForumPermissions.Evaluate(group, level);

        if (ThreadId == 0)
        {
            if (!perms.CanThread || string.IsNullOrWhiteSpace(Subject))
            {
                return;
            }

            var thread = await forumRepository.PostThreadAsync(GuildId, player.Player.Id, Subject.Trim(), Message);

            if (thread != null)
            {
                await client.WriteToStreamAsync(new GuildForumThreadUpdatedWriter
                {
                    GuildId = GuildId,
                    Thread = thread
                });
            }

            return;
        }

        if (!perms.CanPost)
        {
            return;
        }

        var threadRef = await forumRepository.GetThreadRefAsync(ThreadId);

        if (threadRef is not { } tr || tr.GroupId != GuildId || tr.IsLocked)
        {
            return;
        }

        var comment = await forumRepository.PostCommentAsync(GuildId, ThreadId, player.Player.Id, Message);

        if (comment != null)
        {
            await client.WriteToStreamAsync(new GuildForumMessagePostedWriter
            {
                GuildId = GuildId,
                ThreadId = ThreadId,
                Comment = comment
            });
        }
    }
}
