using Ada.API.Interfaces.Game.Groups;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Groups;

namespace Ada.Networking.Events.Handlers.Groups;

[PacketId(EventHandlerId.GuildDeclineMembership)]
public class GuildDeclineMembershipEventHandler(IGroupRepository groupRepository) : INetworkPacketEventHandler
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

        if (group == null || !await groupRepository.HasAdminRightsAsync(group, player.Player.Id))
        {
            return;
        }

        var membership = await groupRepository.GetMembershipAsync(group.Id, UserId);

        if (membership is not { IsPending: true })
        {
            return;
        }

        await groupRepository.RemoveMembershipAsync(group.Id, UserId);
        await client.WriteToStreamAsync(new GuildRefreshMembersWriter { GuildId = group.Id });
    }
}
