using Ada.API.Interfaces.Game.Rooms.Chat.Commands;
using Ada.API.Interfaces.Game.Rooms.Users;

namespace Ada.API;

public abstract class AbstractRoomChatCommand : IRoomChatCommand
{
    public abstract string Trigger { get; }
    public abstract string Description { get; }
    public abstract Task ExecuteAsync(IRoomUser user, IRoomChatCommandParameterReader reader);
    public virtual List<string> PermissionsRequired { get; set; } = [];
    public virtual bool BypassPermissionCheckIfRoomOwner => false;
    public virtual List<string> Parameters { get; } = [];
}