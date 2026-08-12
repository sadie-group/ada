using Ada.API.Interfaces.Game.Groups;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Groups;

namespace Ada.Networking.Events.Handlers.Groups;

[PacketId(EventHandlerId.RequestGuildManage)]
public class RequestGuildManageEventHandler(IGroupRepository groupRepository) : INetworkPacketEventHandler
{
    public int GuildId { get; set; }

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

        await client.WriteToStreamAsync(new GuildManageWriter
        {
            GuildId = group.Id,
            RoomId = group.RoomId,
            RoomName = await groupRepository.GetRoomNameAsync(group.RoomId) ?? "",
            Name = group.Name,
            Description = group.Description,
            ColorA = group.ColorA,
            ColorB = group.ColorB,
            State = (int) group.Type,
            AdminOnlyDecoration = group.AdminOnlyDecoration,
            Badge = group.Badge,
            MemberCount = await groupRepository.GetMemberCountAsync(group.Id)
        });
    }
}
