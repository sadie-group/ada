using Ada.API.Interfaces.Game.Moderation;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Moderation;

[PacketId(EventHandlerId.ReleaseTicket)]
public class ModToolReleaseTicketEventHandler(
    IModerationTicketService ticketService,
    IPlayerRepository playerRepository) : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    public int TicketId { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null || !player.HasPermission(PlayerPermissionName.Moderator))
        {
            return;
        }

        var ticket = await ticketService.TryReleaseAsync(TicketId, player.Player.Id);

        if (ticket == null)
        {
            return;
        }

        await ModToolPickTicketEventHandler.BroadcastToModeratorsAsync(playerRepository, ticket);
    }
}
