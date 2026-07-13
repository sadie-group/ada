using Ada.Core.Enums.Game.Rooms.Users;

namespace Ada.API.Interfaces.Game.Rooms.Services;

public interface IRoomHelperService
{
    RoomUserEmotion GetEmotionFromMessage(string message);
}