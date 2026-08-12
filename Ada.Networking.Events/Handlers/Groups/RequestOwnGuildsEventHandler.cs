using Ada.API.Interfaces.Game.Groups;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Groups;

namespace Ada.Networking.Events.Handlers.Groups;

[PacketId(EventHandlerId.RequestOwnGuilds)]
public class RequestOwnGuildsEventHandler(IGroupRepository groupRepository)
    : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        var groups = await groupRepository.GetGroupsForPlayerAsync(player.Player.Id);

        await client.WriteToStreamAsync(new GuildListWriter
        {
            Guilds = groups
        });
    }
}
