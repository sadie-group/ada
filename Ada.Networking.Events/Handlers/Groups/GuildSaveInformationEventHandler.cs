using Ada.API.Interfaces.Game.Groups;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Groups;

[PacketId(EventHandlerId.GuildSaveInformation)]
public class GuildSaveInformationEventHandler(IGroupRepository groupRepository) : INetworkPacketEventHandler
{
    public int GuildId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null || string.IsNullOrWhiteSpace(Name))
        {
            return;
        }

        var group = await groupRepository.GetByIdAsync(GuildId);

        if (group == null || !await groupRepository.HasAdminRightsAsync(group, player.Player.Id))
        {
            return;
        }

        await groupRepository.UpdateInfoAsync(group.Id, Name.Trim(), Description ?? "");
    }
}
