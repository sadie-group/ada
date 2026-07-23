using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Moderation;

[PacketId(EventHandlerId.ModToolsKickUser)]
public class ModToolKickUserEventHandler(
    IPlayerRepository playerRepository,
    IRoomRepository roomRepository)
    : INetworkPacketEventHandler
{
    public int UserId { get; set; }
    public string Message { get; set; } = "";

    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null || !client.Player.HasPermission(PlayerPermissionName.Moderator))
        {
            return;
        }

        var target = playerRepository.GetPlayerLogicById(UserId);

        if (target == null)
        {
            return;
        }

        var room = roomRepository.TryGetRoomById(target.State.CurrentRoomId);

        if (room == null)
        {
            return;
        }

        await room.UserRepository.TryRemoveAsync(UserId, true, true);
    }
}
