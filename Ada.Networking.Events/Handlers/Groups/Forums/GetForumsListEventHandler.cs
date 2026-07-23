using Ada.API.Interfaces.Game.Groups;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Groups;

namespace Ada.Networking.Events.Handlers.Groups.Forums;

[PacketId(EventHandlerId.GetForumsList)]
public class GetForumsListEventHandler(IGroupForumRepository forumRepository) : INetworkPacketEventHandler
{
    private const int PageSize = 20;

    public int Mode { get; set; }
    public int Offset { get; set; }
    public int Amount { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null)
        {
            return;
        }

        var amount = Amount is > 0 and <= PageSize ? Amount : PageSize;
        var (forums, total) = await forumRepository.GetForumsListAsync(Mode, Offset, amount);

        await client.WriteToStreamAsync(new GuildForumListWriter
        {
            Mode = Mode,
            Total = total,
            StartIndex = Offset,
            Forums = forums
        });
    }
}
