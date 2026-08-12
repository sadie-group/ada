using Ada.API.Interfaces.Game.Guides;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Guides;

namespace Ada.Networking.Events.Handlers.Guides;

[PacketId(EventHandlerId.GuideAssistanceRequest)]
public class GuideAssistanceRequestEventHandler(
    IGuideSessionService sessionService,
    IPlayerRepository playerRepository) : INetworkPacketEventHandler
{
    private const int _maxRequestLength = 256;

    public int RequestType { get; set; }
    public string? Message { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        if (sessionService.GetSessionForPlayer(player.Player.Id) != null)
        {
            await client.WriteToStreamAsync(new GuideSessionErrorWriter
            {
                ErrorCode = (int) GuideSessionError.AlreadyInSession
            });

            return;
        }

        var message = (Message ?? string.Empty).Trim();

        if (message.Length > _maxRequestLength)
        {
            message = message[.._maxRequestLength];
        }

        var session = sessionService.CreateSession(player.Player.Id, RequestType, message);

        var helperId = sessionService.TryAssignHelper(session, playerRepository.GetAll());

        if (helperId == null)
        {
            sessionService.End(session);

            await client.WriteToStreamAsync(new GuideSessionErrorWriter
            {
                ErrorCode = (int) GuideSessionError.NoHelpersAvailable
            });

            return;
        }

        await client.WriteToStreamAsync(new GuideSessionAttachedWriter
        {
            IsHelper = false,
            TourType = 1,
            HelpRequest = message,
            SecondsRemaining = sessionService.AverageWaitSeconds
        });

        var helper = playerRepository.GetPlayerLogicById(helperId.Value);

        if (helper?.NetworkObject == null)
        {
            return;
        }

        await helper.NetworkObject.WriteToStreamAsync(new GuideSessionAttachedWriter
        {
            IsHelper = true,
            TourType = 1,
            HelpRequest = message,
            SecondsRemaining = 60
        });
    }
}
