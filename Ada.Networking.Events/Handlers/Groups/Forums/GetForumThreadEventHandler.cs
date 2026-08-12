using Ada.API.Interfaces.Game.Groups;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Groups;

namespace Ada.Networking.Events.Handlers.Groups.Forums;

[PacketId(EventHandlerId.GetForumThread)]
public class GetForumThreadEventHandler(
    IGroupRepository groupRepository,
    IGroupForumRepository forumRepository) : INetworkPacketEventHandler
{
    private const int PageSize = 20;

    public int GuildId { get; set; }
    public int ThreadId { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null)
        {
            return;
        }

        if (!await ForumPermissions.CanReadForumAsync(groupRepository, GuildId, client.Player.Player.Id))
        {
            return;
        }

        var (comments, _) = await forumRepository.GetCommentsAsync(GuildId, ThreadId, 0, PageSize);

        await client.WriteToStreamAsync(new GuildForumCommentsWriter
        {
            GuildId = GuildId,
            ThreadId = ThreadId,
            StartIndex = 0,
            Comments = comments
        });
    }
}
