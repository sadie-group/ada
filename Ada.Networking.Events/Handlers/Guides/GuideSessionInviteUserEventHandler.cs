using Ada.API.Interfaces.Game.Guides;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Guides;

namespace Ada.Networking.Events.Handlers.Guides;

[PacketId(EventHandlerId.GuideSessionInviteUser)]
public class GuideSessionInviteUserEventHandler(
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

        var room = client.RoomUser?.Room;

        if (room == null)
        {
            return;
        }

        var requester = playerRepository.GetPlayerLogicById(session.RequesterId);

        if (requester?.NetworkObject == null)
        {
            return;
        }

        await requester.NetworkObject.WriteToStreamAsync(new GuideSessionInvitedToGuideRoomWriter
        {
            RoomId = (int) room.Room.Id,
            RoomName = room.Room.Name
        });
    }
}
