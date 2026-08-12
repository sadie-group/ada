using Ada.API.Interfaces.Game.Guides;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Guides;

namespace Ada.Networking.Events.Handlers.Guides;

[PacketId(EventHandlerId.GuideSessionTyping)]
public class GuideSessionTypingEventHandler(
    IGuideSessionService sessionService,
    IPlayerRepository playerRepository) : INetworkPacketEventHandler
{
    public bool Typing { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        var session = sessionService.GetSessionForPlayer(player.Player.Id);

        if (session is not { State: GuideSessionState.Active })
        {
            return;
        }

        var partner = GuideSessionBroadcast.PartnerOf(session, player.Player.Id, playerRepository);

        if (partner?.NetworkObject == null)
        {
            return;
        }

        await partner.NetworkObject.WriteToStreamAsync(new GuideSessionPartnerIsTypingWriter
        {
            Typing = Typing
        });
    }
}
