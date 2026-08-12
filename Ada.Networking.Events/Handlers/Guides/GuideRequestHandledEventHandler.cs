using Ada.API.Interfaces.Game.Guides;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Guides;

namespace Ada.Networking.Events.Handlers.Guides;

[PacketId(EventHandlerId.GuideRequestHandled)]
public class GuideRequestHandledEventHandler(
    IGuideSessionService sessionService,
    IPlayerRepository playerRepository) : INetworkPacketEventHandler
{
    public bool Accepted { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        var helperId = player.Player.Id;
        var session = sessionService.GetPendingSessionForHelper(helperId);

        if (session == null)
        {
            return;
        }

        var requester = playerRepository.GetPlayerLogicById(session.RequesterId);

        if (!Accepted)
        {
            sessionService.Decline(session);

            var reassignedTo = sessionService.TryAssignHelper(session, playerRepository.GetAll());

            if (reassignedTo == null)
            {
                sessionService.End(session);

                if (requester?.NetworkObject != null)
                {
                    await requester.NetworkObject.WriteToStreamAsync(new GuideSessionErrorWriter
                    {
                        ErrorCode = (int) GuideSessionError.NoHelpersAvailable
                    });
                }

                return;
            }

            var reassigned = playerRepository.GetPlayerLogicById(reassignedTo.Value);

            if (reassigned?.NetworkObject != null)
            {
                await reassigned.NetworkObject.WriteToStreamAsync(new GuideSessionAttachedWriter
                {
                    IsHelper = true,
                    TourType = 1,
                    HelpRequest = session.HelpRequest,
                    SecondsRemaining = 60
                });
            }

            return;
        }

        if (!sessionService.Accept(session, helperId) || requester == null)
        {
            return;
        }

        var started = new GuideSessionStartedWriter
        {
            RequesterId = (int) requester.Player.Id,
            RequesterUsername = requester.Player.Username,
            RequesterFigureCode = requester.Player.AvatarData?.FigureCode ?? "",
            HelperId = (int) player.Player.Id,
            HelperUsername = player.Player.Username,
            HelperFigureCode = player.Player.AvatarData?.FigureCode ?? ""
        };

        await client.WriteToStreamAsync(started);

        if (requester.NetworkObject != null)
        {
            await requester.NetworkObject.WriteToStreamAsync(started);
        }
    }
}
