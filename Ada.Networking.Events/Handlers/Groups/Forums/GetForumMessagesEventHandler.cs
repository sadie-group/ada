using Ada.API.Interfaces.Game.Groups;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Groups;

namespace Ada.Networking.Events.Handlers.Groups.Forums;

[PacketId(EventHandlerId.GetForumMessages)]
public class GetForumMessagesEventHandler(
    IGroupRepository groupRepository,
    IGroupForumRepository forumRepository) : INetworkPacketEventHandler
{
    private const int _maxPageSize = 20;

    public int GuildId { get; set; }
    public int ThreadId { get; set; }
    public int Index { get; set; }
    public int Limit { get; set; }

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

        var amount = Limit is > 0 and <= _maxPageSize ? Limit : _maxPageSize;
        var (comments, _) = await forumRepository.GetCommentsAsync(GuildId, ThreadId, Index, amount);

        await client.WriteToStreamAsync(new GuildForumCommentsWriter
        {
            GuildId = GuildId,
            ThreadId = ThreadId,
            StartIndex = Index,
            Comments = comments
        });
    }
}
