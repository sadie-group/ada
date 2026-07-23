using Ada.API.Interfaces.Game.Groups;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Groups;

[PacketId(EventHandlerId.GuildSaveColors)]
public class GuildSaveColorsEventHandler(IGroupRepository groupRepository) : INetworkPacketEventHandler
{
    public int GuildId { get; set; }
    public int ColorOne { get; set; }
    public int ColorTwo { get; set; }

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

        await groupRepository.UpdateColorsAsync(group.Id, ColorOne, ColorTwo);
    }
}
