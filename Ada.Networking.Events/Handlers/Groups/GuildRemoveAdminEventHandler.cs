using Ada.API.Interfaces.Game.Groups;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Groups;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Groups;

namespace Ada.Networking.Events.Handlers.Groups;

[PacketId(EventHandlerId.GuildRemoveAdmin)]
public class GuildRemoveAdminEventHandler(IGroupRepository groupRepository) : INetworkPacketEventHandler
{
    public int GuildId { get; set; }
    public int UserId { get; set; }

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

        await groupRepository.SetRankAsync(group.Id, UserId, GroupMemberRank.Member);
        await client.WriteToStreamAsync(new GuildRefreshMembersWriter { GuildId = group.Id });
    }
}
