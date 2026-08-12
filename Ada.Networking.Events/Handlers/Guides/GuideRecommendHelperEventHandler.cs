using Ada.API.Interfaces.Game.Guides;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Guides;
using Microsoft.Extensions.Logging;

namespace Ada.Networking.Events.Handlers.Guides;

[PacketId(EventHandlerId.GuideRecommendHelper)]
public class GuideRecommendHelperEventHandler(
    IGuideSessionService sessionService,
    IPlayerRepository playerRepository,
    ILogger<GuideRecommendHelperEventHandler> logger) : INetworkPacketEventHandler
{
    public bool Recommend { get; set; }

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

        logger.LogInformation(
            "Player {PlayerId} {Verdict} helper {HelperId} after a guide session",
            player.Player.Id,
            Recommend ? "recommended" : "did not recommend",
            session.HelperId);

        await GuideSessionBroadcast.ToBothAsync(session, playerRepository, new GuideSessionEndedWriter
        {
            Reason = (int) GuideSessionEndReason.HelperEnded
        });

        sessionService.End(session);
    }
}
