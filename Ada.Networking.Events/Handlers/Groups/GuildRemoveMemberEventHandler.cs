using Ada.API.Interfaces.Game.Groups;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Groups;

namespace Ada.Networking.Events.Handlers.Groups;

[PacketId(EventHandlerId.GuildRemoveMember)]
public class GuildRemoveMemberEventHandler(IGroupRepository groupRepository) : INetworkPacketEventHandler
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

        if (group == null)
        {
            return;
        }

        var isSelf = UserId == player.Player.Id;

        if (UserId == group.PlayerId)
        {
            return;
        }

        if (!isSelf && !await groupRepository.HasAdminRightsAsync(group, player.Player.Id))
        {
            return;
        }

        await groupRepository.RemoveMembershipAsync(group.Id, UserId);
        await client.WriteToStreamAsync(new GuildRefreshMembersWriter { GuildId = group.Id });
    }
}
