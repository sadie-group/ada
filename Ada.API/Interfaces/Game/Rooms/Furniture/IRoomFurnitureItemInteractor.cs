using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms.Users;

namespace Ada.API.Interfaces.Game.Rooms.Furniture;

public interface IRoomFurnitureItemInteractor
{
    List<string> InteractionTypes { get; }
    Task OnTriggerAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser);
    Task OnPlaceAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser);
    Task OnPickUpAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser);
    Task OnMoveAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser);
    Task OnWalkedOnAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser);
    Task OnWalkedOffAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser);
}