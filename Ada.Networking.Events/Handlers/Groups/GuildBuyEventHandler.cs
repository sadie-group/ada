using Ada.API.DTOs.Groups;
using Ada.API.Interfaces.Game.Groups;
using Ada.API.Interfaces.Game.WordFilter;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Groups;
using Ada.Core.Enums.Game.WordFilter;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Extensions;
using Ada.Networking.Writers.Groups;

namespace Ada.Networking.Events.Handlers.Groups;

[PacketId(EventHandlerId.GuildBuy)]
public class GuildBuyEventHandler(
    IGroupRepository groupRepository,
    IWordFilterService wordFilterService) : INetworkPacketEventHandler
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int RoomId { get; set; }
    public int ColorOne { get; set; }
    public int ColorTwo { get; set; }
    public List<GroupBadgePart> Parts { get; set; } = [];

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            await client.WriteToStreamAsync(new GuildEditFailWriter { ErrorCode = 1 });
            return;
        }

        if (!await groupRepository.PlayerOwnsRoomAsync(RoomId, player.Player.Id) ||
            await groupRepository.RoomHasGroupAsync(RoomId))
        {
            await client.WriteToStreamAsync(new GuildEditFailWriter { ErrorCode = 0 });
            return;
        }

        var nameResult = wordFilterService.Filter(Name.Trim(), WordFilterContext.RoomName);
        var descriptionResult = wordFilterService.Filter(Description ?? "", WordFilterContext.RoomDescription);

        if (nameResult.IsBlocked || nameResult.IsShadowBlocked ||
            descriptionResult.IsBlocked || descriptionResult.IsShadowBlocked)
        {
            await client.WriteToStreamAsync(new GuildEditFailWriter { ErrorCode = 1 });
            return;
        }

        var name = nameResult.FilteredText.Truncate(GroupTextLimits.MaxNameLength);
        var description = descriptionResult.FilteredText.Truncate(GroupTextLimits.MaxDescriptionLength);

        var badge = GroupBadgeCodec.Build(Parts);

        var groupId = await groupRepository.CreateGroupAsync(
            player.Player.Id, RoomId, name, description, badge,
            ColorOne, ColorTwo, GroupType.Open);

        await client.WriteToStreamAsync(new GuildBoughtWriter
        {
            RoomId = RoomId,
            GuildId = groupId
        });
    }
}
