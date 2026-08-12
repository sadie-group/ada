using Ada.API.Interfaces.Game.Guides;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Guides;

namespace Ada.Networking.Events.Handlers.Guides;

[PacketId(EventHandlerId.GuideSessionVisitUser)]
public class GuideSessionVisitUserEventHandler(
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

        if (session is not { State: GuideSessionState.Active } ||
            session.HelperId != player.Player.Id)
        {
            return;
        }

        var requester = playerRepository.GetPlayerLogicById(session.RequesterId);
        var roomId = requester?.NetworkObject is INetworkClient requesterClient
            ? requesterClient.RoomUser?.Room.Room.Id ?? 0
            : 0;

        await client.WriteToStreamAsync(new GuideSessionRequesterRoomWriter
        {
            RoomId = (int) roomId
        });
    }
}
