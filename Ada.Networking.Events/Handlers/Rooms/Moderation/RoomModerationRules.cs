using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Users;

namespace Ada.Networking.Events.Handlers.Rooms.Moderation;

public static class RoomModerationRules
{
    public static bool CanActOn(
        IRoomLogic room,
        IRoomUser actor,
        long targetPlayerId,
        out IRoomUser? target)
    {
        target = null;

        if (targetPlayerId == actor.Player.Player.Id)
        {
            return false;
        }

        if (room.Room.OwnerId == targetPlayerId)
        {
            return false;
        }

        var found = room.UserRepository.GetAll()
            .FirstOrDefault(x => x.Player.Player.Id == targetPlayerId);

        if (found == null)
        {
            return false;
        }

        if (found.ControllerLevel >= actor.ControllerLevel)
        {
            return false;
        }

        target = found;

        return true;
    }
}
