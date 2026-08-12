using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Networking.Client;

namespace Ada.Game.Rooms;

public static class RoomContextResolver
{
    public static bool TryResolveRoomObjectsForClient(
        IRoomRepository roomRepository,
        INetworkClient client,
        out IRoomLogic room,
        out IRoomUser user)
    {
        var player = client.Player;
        
        if (player == null)
        {
            room = null!;
            user = null!;
            return false;
        }

        var roomId = player.State.CurrentRoomId;
        var roomObject = roomRepository.TryGetRoomById(roomId);

        if (roomObject == null || client.RoomUser == null)
        {
            room = null!;
            user = null!;
            return false;
        }

        room = roomObject;
        user = client.RoomUser;
        return true;
    }
}
