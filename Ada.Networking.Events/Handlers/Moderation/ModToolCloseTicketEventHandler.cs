using Ada.API.Interfaces.Game.Moderation;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Moderation;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Moderation;

[PacketId(EventHandlerId.CloseTicket)]
public class ModToolCloseTicketEventHandler(
    IModerationTicketService ticketService,
    IPlayerRepository playerRepository) : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    public int Resolution { get; set; }
    public List<int> TicketIds { get; set; } = [];

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null || !player.HasPermission(PlayerPermissionName.Moderator))
        {
            return;
        }

        var resolution = Resolution switch
        {
            1 => ModerationTicketResolution.Useless,
            2 => ModerationTicketResolution.Abusive,
            3 => ModerationTicketResolution.Resolved,
            _ => ModerationTicketResolution.Resolved
        };

        foreach (var ticketId in TicketIds)
        {
            var ticket = await ticketService.TryCloseAsync(ticketId, player.Player.Id, resolution);

            if (ticket == null)
            {
                continue;
            }

            await ModToolPickTicketEventHandler.BroadcastToModeratorsAsync(playerRepository, ticket);
        }
    }
}
