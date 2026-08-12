using Ada.API.Interfaces.Game.Groups;
using Ada.API.Interfaces.Game.WordFilter;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.WordFilter;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Extensions;

namespace Ada.Networking.Events.Handlers.Groups;

[PacketId(EventHandlerId.GuildSaveInformation)]
public class GuildSaveInformationEventHandler(
    IGroupRepository groupRepository,
    IWordFilterService wordFilterService) : INetworkPacketEventHandler
{
    public int GuildId { get; set; }
    private string Name { get; set; } = "";
    private string Description { get; set; } = "";

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

        var name = wordFilterService.Filter(Name.Trim(), WordFilterContext.RoomName);
        var description = wordFilterService.Filter(Description ?? "", WordFilterContext.RoomDescription);

        if (name.IsBlocked || name.IsShadowBlocked ||
            description.IsBlocked || description.IsShadowBlocked)
        {
            return;
        }

        await groupRepository.UpdateInfoAsync(
            group.Id,
            name.FilteredText.Truncate(GroupTextLimits.MaxNameLength),
            description.FilteredText.Truncate(GroupTextLimits.MaxDescriptionLength));
    }
}
