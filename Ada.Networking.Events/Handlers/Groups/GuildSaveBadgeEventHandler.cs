using Ada.API.DTOs.Groups;
using Ada.API.Interfaces.Game.Groups;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Groups;

[PacketId(EventHandlerId.GuildSaveBadge)]
public class GuildSaveBadgeEventHandler(IGroupRepository groupRepository) : INetworkPacketEventHandler
{
    public int GuildId { get; set; }
    public List<GroupBadgePart> Parts { get; set; } = [];

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null || Parts.Count == 0)
        {
            return;
        }

        var group = await groupRepository.GetByIdAsync(GuildId);

        if (group == null || !await groupRepository.HasAdminRightsAsync(group, player.Player.Id))
        {
            return;
        }

        await groupRepository.UpdateBadgeAsync(group.Id, GroupBadgeCodec.Build(Parts));
    }
}
