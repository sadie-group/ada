using Ada.API.Interfaces.Game.Guides;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Guides;

namespace Ada.Networking.Events.Handlers.Guides;

[PacketId(EventHandlerId.GuideSessionMessage)]
public class GuideSessionMessageEventHandler(
    IGuideSessionService sessionService,
    IPlayerRepository playerRepository) : INetworkPacketEventHandler
{
    private const int _maxMessageLength = 256;

    public string? Message { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null || string.IsNullOrWhiteSpace(Message))
        {
            return;
        }

        var session = sessionService.GetSessionForPlayer(player.Player.Id);

        if (session is not { State: GuideSessionState.Active })
        {
            return;
        }

        var message = Message.Trim();

        if (message.Length > _maxMessageLength)
        {
            message = message[.._maxMessageLength];
        }

        var writer = new GuideSessionMessageWriter
        {
            Message = message,
            SenderId = (int) player.Player.Id
        };

        await GuideSessionBroadcast.ToBothAsync(session, playerRepository, writer);
    }
}
