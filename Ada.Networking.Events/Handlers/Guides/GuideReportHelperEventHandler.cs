using Ada.API.Interfaces.Game.Guides;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Guides;
using Microsoft.Extensions.Logging;

namespace Ada.Networking.Events.Handlers.Guides;

[PacketId(EventHandlerId.GuideReportHelper)]
public class GuideReportHelperEventHandler(
    IGuideSessionService sessionService,
    IPlayerRepository playerRepository,
    ILogger<GuideReportHelperEventHandler> logger) : INetworkPacketEventHandler
{
    private const int _maxReasonLength = 256;

    public string? Reason { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        var session = sessionService.GetSessionForPlayer(player.Player.Id);

        if (session == null || session.RequesterId != player.Player.Id)
        {
            return;
        }

        var reason = (Reason ?? string.Empty).Trim();

        if (reason.Length > _maxReasonLength)
        {
            reason = reason[.._maxReasonLength];
        }

        logger.LogWarning(
            "Player {PlayerId} reported helper {HelperId} after a guide session: {Reason}",
            player.Player.Id,
            session.HelperId,
            reason);

        await GuideSessionBroadcast.ToBothAsync(session, playerRepository, new GuideSessionEndedWriter
        {
            Reason = (int) GuideSessionEndReason.HelperEnded
        });

        sessionService.End(session);
    }
}
