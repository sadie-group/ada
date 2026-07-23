using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms.Users;

namespace Ada.API.Interfaces.Game.Rooms.Services.Wired;

public interface IWiredConditionStrategy
{
    string InteractionType { get; }
    
    bool IsSatisfied(
        IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto condition,
        IRoomUser? userWhoTriggered);
}
