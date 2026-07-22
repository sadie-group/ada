namespace Ada.Core.Enums.Game.Rooms.Furniture;

public enum WiredEffectCode
{
    ToggleFurnitureState = 0,
    TimerReset = 1,
    MoveRotateFurniture = 4,
    ShowMessage = 7,
    TeleportToFurniture = 8,
    MoveFurnitureToClosestUser = 11,
    FleeFromClosestUser = 12,
    ChangeFurnitureDirection = 13,
    CallAnotherStack = 18,
    KickUser = 19,
    MuteTriggerer = 20,
    BotChangedClothes = 26,
    BotTalkToAvatar = 27
}