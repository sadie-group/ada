using System.Globalization;
using System.Text;
using Ada.API.DTOs.Groups;
using Ada.API.Interfaces.Game.Groups;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Groups;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Groups;

namespace Ada.Networking.Events.Handlers.Groups;

[PacketId(EventHandlerId.GuildBuy)]
public class GuildBuyEventHandler(IGroupRepository groupRepository) : INetworkPacketEventHandler
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

        var badge = BuildBadge(Parts);

        var groupId = await groupRepository.CreateGroupAsync(
            player.Player.Id, RoomId, Name.Trim(), Description ?? "", badge,
            ColorOne, ColorTwo, GroupType.Open);

        await client.WriteToStreamAsync(new GuildBoughtWriter
        {
            RoomId = RoomId,
            GuildId = groupId
        });
    }

    private static string BuildBadge(IReadOnlyList<GroupBadgePart> parts)
    {
        var badge = new StringBuilder();

        for (var i = 0; i < parts.Count; i++)
        {
            var part = parts[i];

            badge.Append(i == 0 ? 'b' : 's');
            badge.Append(part.PartId.ToString("D3", CultureInfo.InvariantCulture));
            badge.Append(part.ColorId.ToString("D2", CultureInfo.InvariantCulture));
            badge.Append(part.Position.ToString(CultureInfo.InvariantCulture));
        }

        return badge.ToString();
    }
}
