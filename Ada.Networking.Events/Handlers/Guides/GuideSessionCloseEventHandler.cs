using Ada.API.Interfaces.Game.Guides;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Guides;

namespace Ada.Networking.Events.Handlers.Guides;

[PacketId(EventHandlerId.GuideSessionClose)]
public class GuideSessionCloseEventHandler(
    IGuideSessionService sessionService,
    IPlayerRepository playerRepository) : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        var session = sessionService.GetSessionForPlayer(player.Player.Id);

        if (session == null)
        {
            return;
        }

        var reason = session.RequesterId == player.Player.Id
            ? GuideSessionEndReason.RequesterCancelled
            : GuideSessionEndReason.HelperEnded;

        await GuideSessionBroadcast.ToBothAsync(session, playerRepository, new GuideSessionEndedWriter
        {
            Reason = (int) reason
        });

        sessionService.End(session);
    }
}
