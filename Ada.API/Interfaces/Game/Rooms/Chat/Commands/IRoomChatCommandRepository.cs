namespace Ada.API.Interfaces.Game.Rooms.Chat.Commands;

public interface IRoomChatCommandRepository
{
    IRoomChatCommand? TryGetCommandByTriggerWord(string trigger);
    ICollection<IRoomChatCommand> GetRegisteredCommands();
}