using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms.Users;

namespace Ada.API.Interfaces.Game.Rooms.Furniture;

public abstract class AbstractRoomFurnitureItemInteractor : IRoomFurnitureItemInteractor
{
    public abstract List<string> InteractionTypes { get; }
    
    public virtual Task OnTriggerAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnPlaceAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnPickUpAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnMoveAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnWalkedOnAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnWalkedOffAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser)
    {
        return Task.CompletedTask;
    }
}