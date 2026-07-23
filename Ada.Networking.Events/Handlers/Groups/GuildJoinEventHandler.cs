using Ada.API.Interfaces.Game.Groups;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Groups;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Groups;

namespace Ada.Networking.Events.Handlers.Groups;

[PacketId(EventHandlerId.GuildJoin)]
public class GuildJoinEventHandler(IGroupRepository groupRepository) : INetworkPacketEventHandler
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

        if (group == null)
        {
            return;
        }

        var existing = await groupRepository.GetMembershipAsync(group.Id, player.Player.Id);

        if (existing != null)
        {
            return;
        }

        switch (group.Type)
        {
            case GroupType.Open:
                await groupRepository.AddMembershipAsync(group.Id, player.Player.Id, GroupMemberRank.Member, false);
                break;
            case GroupType.Request:
                await groupRepository.AddMembershipAsync(group.Id, player.Player.Id, GroupMemberRank.Member, true);
                break;
            case GroupType.Closed:
            default:
                await client.WriteToStreamAsync(new GuildJoinFailWriter { Code = 2 });
                return;
        }

        await client.WriteToStreamAsync(new GuildRefreshMembersWriter { GuildId = group.Id });
    }
}
