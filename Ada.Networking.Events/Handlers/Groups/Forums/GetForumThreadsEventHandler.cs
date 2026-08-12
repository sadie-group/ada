using Ada.API.Interfaces.Game.Groups;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Groups;

namespace Ada.Networking.Events.Handlers.Groups.Forums;

[PacketId(EventHandlerId.GetForumThreads)]
public class GetForumThreadsEventHandler(
    IGroupRepository groupRepository,
    IGroupForumRepository forumRepository) : INetworkPacketEventHandler
{
    private const int PageSize = 20;

    public int GuildId { get; set; }
    public int Index { get; set; }

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

        var threads = await forumRepository.GetThreadsAsync(GuildId, Index, PageSize);

        await client.WriteToStreamAsync(new GuildForumThreadsWriter
        {
            GuildId = GuildId,
            StartIndex = Index,
            Threads = threads
        });
    }
}
