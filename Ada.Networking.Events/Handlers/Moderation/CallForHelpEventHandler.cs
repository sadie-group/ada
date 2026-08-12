using Ada.API.Interfaces.Game.Moderation;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Moderation;

namespace Ada.Networking.Events.Handlers.Moderation;

[PacketId(EventHandlerId.CallForHelp)]
public class CallForHelpEventHandler(
    IModerationTicketService ticketService,
    IPlayerRepository playerRepository) : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    public string Message { get; set; } = "";
    public int CategoryId { get; set; }
    public int ReportedUserId { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Message) || Message.Length > 512)
        {
            await client.WriteToStreamAsync(new CallForHelperErrorWriter { ErrorCode = 0 });
            return;
        }

        long? reportedId = ReportedUserId > 0 ? ReportedUserId : null;
        int? roomId = player.State.CurrentRoomId > 0 ? player.State.CurrentRoomId : null;

        var ticket = await ticketService.CreateAsync(
            player.Player.Id, reportedId, roomId, CategoryId, Message);

        if (ticket == null)
        {
            await client.WriteToStreamAsync(new CallForHelperErrorWriter { ErrorCode = 1 });
            return;
        }

        await client.WriteToStreamAsync(new ModerationTicketResponseWriter { Result = 1 });

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
