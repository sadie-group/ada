using Ada.API.Interfaces.Game.Rooms.Users;

namespace Ada.API.Interfaces.Game.Rooms.Chat.Commands;

public interface IRoomChatCommandParameterReader
{
    bool GetWord(out string? value);
    bool GetSentence(out string? value);
    bool GetInt(out int value);

    bool TryGetUser(
        IRoomUserRepository userRepository, 
        out int userId, 
        out IRoomUser? user);
}