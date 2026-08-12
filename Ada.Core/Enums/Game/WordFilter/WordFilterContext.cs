namespace Ada.Core.Enums.Game.WordFilter;

[Flags]
public enum WordFilterContext
{
    None = 0,
    Chat = 1,
    Whisper = 2,
    RoomName = 4,
    RoomDescription = 8,
    RoomTag = 16,
    All = Chat | Whisper | RoomName | RoomDescription | RoomTag
}
