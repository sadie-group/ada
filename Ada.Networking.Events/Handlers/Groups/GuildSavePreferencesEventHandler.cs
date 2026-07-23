using Ada.API.Interfaces.Game.Groups;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Groups;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Groups;

[PacketId(EventHandlerId.GuildSavePreferences)]
public class GuildSavePreferencesEventHandler(IGroupRepository groupRepository) : INetworkPacketEventHandler
{
    public int GuildId { get; set; }
    public int State { get; set; }
    public int OnlyAdminsDecorate { get; set; }

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

        var type = State is >= 0 and <= 2 ? (GroupType) State : GroupType.Open;

        await groupRepository.UpdatePreferencesAsync(group.Id, type, OnlyAdminsDecorate == 0);
    }
}
