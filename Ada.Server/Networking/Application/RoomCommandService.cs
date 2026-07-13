using Ada.API.Interfaces.Game.Rooms.Chat.Commands;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Rooms;
using Ada.Game.Rooms.Chat.Commands;

namespace Ada.Server.Networking.Application;

public static class RoomCommandService
{
    public static async Task<bool> TryExecuteAsync(
        IRoomChatCommandRepository repository,
        string message,
        IRoomUser roomUser)
    {
        var triggerWord = message.Split(' ')[0][1..].ToLower();
        var command = repository.TryGetCommandByTriggerWord(triggerWord);

        if (command == null)
        {
            return false;
        }

        if (command.BypassPermissionCheckIfRoomOwner &&
            roomUser.ControllerLevel != RoomControllerLevel.Owner)
        {
            return false;
        }

        if (!command.PermissionsRequired.All(roomUser.Player.HasPermission))
        {
            return false;
        }

        var args = new Queue<string>(message.Split(' ').Skip(1));
        await command.ExecuteAsync(roomUser, new RoomChatCommandParameterReader(args));
        
        return true;
    }
}