namespace Ada.Core.Enums.Game.Rooms.Furniture;

public enum WiredConditionCode
{
    StatesMatch = 0,
    FurnitureHasUsers = 1,
    TriggererOnFurniture = 2,
    TimeElapsedMore = 3,
    TimeElapsedLess = 4,
    UserCountInRoom = 5,
    TriggererInTeam = 6,
    FurnitureHasFurniture = 7,
    FurnitureTypeMatches = 8,
    TriggererInGroup = 10,
    TriggererWearsBadge = 11,
    TriggererWearsEffect = 12,
    NotStatesMatch = 13,
    NotFurnitureHasUsers = 14,
    NotTriggererOnFurniture = 15,
    NotUserCountInRoom = 16,
    NotTriggererInTeam = 17,
    NotFurnitureHasFurniture = 18,
    NotFurnitureTypeMatches = 19,
    NotTriggererInGroup = 21,
    NotTriggererWearsBadge = 22,
    NotTriggererWearsEffect = 23,
    DateRangeActive = 24,
    TriggererHasHandItem = 25
}
