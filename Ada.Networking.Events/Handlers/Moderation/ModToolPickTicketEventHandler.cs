using Ada.API.Interfaces.Game.Moderation;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Moderation;

namespace Ada.Networking.Events.Handlers.Moderation;

[PacketId(EventHandlerId.PickTicket)]
public abstract class ModToolPickTicketEventHandler(
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

        var ticket = await ticketService.TryPickAsync(TicketId, player.Player.Id, player.Player.Username);

        if (ticket == null)
        {
            await client.WriteToStreamAsync(new CallForHelperErrorWriter { ErrorCode = 2 });
            return;
        }

        await BroadcastToModeratorsAsync(playerRepository, ticket);
    }

    internal static async Task BroadcastToModeratorsAsync(
        IPlayerRepository playerRepository,
        Ada.API.DTOs.Moderation.ModerationTicketDto ticket)
    {
        foreach (var moderator in playerRepository.GetAll())
        {
            if (moderator.NetworkObject == null || !moderator.HasPermission(PlayerPermissionName.Moderator))
            {
                continue;
            }

            await moderator.NetworkObject.WriteToStreamAsync(new ModerationTicketIssueWriter { Ticket = ticket });
        }
    }
}
