using Ada.API.Interfaces.Game.Groups;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Groups;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Groups;

namespace Ada.Networking.Events.Handlers.Groups.Forums;

[PacketId(EventHandlerId.ForumModerateMessage)]
public class ForumModerateMessageEventHandler(
    IGroupRepository groupRepository,
    IGroupForumRepository forumRepository)
    : INetworkPacketEventHandler
{
    public int GuildId { get; set; }
    public int ThreadId { get; set; }
    public int MessageId { get; set; }
    public int State { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
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

        if (!ForumPermissions.Evaluate(group, level).CanModerate)
        {
            return;
        }

        var state = Enum.IsDefined(typeof(ForumMessageState), State)
            ? (ForumMessageState) State
            : ForumMessageState.Hidden;

        await forumRepository.ModerateCommentAsync(MessageId, state, player.Player.Id);

        var comment = await forumRepository.GetCommentAsync(GuildId, MessageId);

        if (comment != null)
        {
            await client.WriteToStreamAsync(new GuildForumMessageUpdatedWriter
            {
                GuildId = GuildId,
                ThreadId = ThreadId,
                Comment = comment
            });
        }
    }
}
