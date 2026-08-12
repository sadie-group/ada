using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Rooms.Rights;

namespace Ada.Networking.Events.Handlers.Rooms.Rights;

[PacketId(EventHandlerId.GetRoomRights)]
public class GetRoomRightsEventHandler(
    IRoomRepository roomRepository,
    IPlayerRepository playerRepository) : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        var room = roomRepository.TryGetRoomById(player.State.CurrentRoomId);

        if (room == null || room.Room.OwnerId != player.Player.Id)
        {
            return;
        }

        var players = new List<KeyValuePair<long, string>>();

        foreach (var right in room.Room.PlayerRights)
        {
            var username = room.UserRepository.TryGetById((int) right.PlayerId, out var roomUser)
                ? roomUser!.Player.Player.Username
                : await playerRepository.GetPlayerUsernameByIdAsync(right.PlayerId);

            if (string.IsNullOrEmpty(username))
            {
                continue;
            }

            players.Add(new KeyValuePair<long, string>(right.PlayerId, username));
        }

        await client.WriteToStreamAsync(new RoomRightsListWriter
        {
            RoomId = room.Room.Id,
            Players = players
        });
    }
}
