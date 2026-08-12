using Ada.API.Interfaces.Game.Moderation;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Moderation;

[PacketId(EventHandlerId.ModToolsIssueChangeTopic)]
public class ModToolIssueChangeTopicEventHandler(
    IModerationTicketService ticketService) : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    public int TicketId { get; set; }
    public int CategoryId { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null || !client.Player.HasPermission(PlayerPermissionName.Moderator))
        {
            return;
        }

        await ticketService.TryChangeTopicAsync(TicketId, client.Player.Player.Id, CategoryId);
    }
}
