using Ada.API.Interfaces.Game.Guides;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Guides;

namespace Ada.Networking.Events.Handlers.Guides;

[PacketId(EventHandlerId.GuideRequestCancelled)]
public class GuideRequestCancelledEventHandler(
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

        if (session == null || session.RequesterId != player.Player.Id)
        {
            return;
        }

        if (session.HelperId is { } helperId)
        {
            var helper = playerRepository.GetPlayerLogicById(helperId);

            if (helper?.NetworkObject != null)
            {
                await helper.NetworkObject.WriteToStreamAsync(new GuideSessionDetachedWriter());
            }
        }

        sessionService.End(session);

        await client.WriteToStreamAsync(new GuideSessionDetachedWriter());
    }
}
