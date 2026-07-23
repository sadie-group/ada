using Ada.API.Interfaces.Game.Groups;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Groups;

namespace Ada.Networking.Events.Handlers.Groups;

[PacketId(EventHandlerId.RequestGuildFurniWidget)]
public class RequestGuildFurniWidgetEventHandler(IGroupRepository groupRepository)
    : INetworkPacketEventHandler
{
    public int ItemId { get; set; }
    public int GuildId { get; set; }

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

        await client.WriteToStreamAsync(new GuildFurniWidgetWriter
        {
            ItemId = ItemId,
            GuildId = group.Id,
            GuildName = group.Name,
            RoomId = group.RoomId,
            UserJoined = await groupRepository.IsMemberAsync(group.Id, player.Player.Id),
            HasForum = group.HasForum
        });
    }
}
