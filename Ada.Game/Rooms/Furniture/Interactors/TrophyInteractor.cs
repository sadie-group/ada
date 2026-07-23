using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;

namespace Ada.Game.Rooms.Furniture.Interactors;

public class TrophyInteractor : AbstractRoomFurnitureItemInteractor
{
    public override List<string> InteractionTypes => [
        FurnitureItemInteractionType.Trophy
    ];

    public override Task OnTriggerAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser)
    {
        // The engraving lives in the item's metadata and is rendered by the
        // client; using a trophy must not cycle its state or the engraving
        // would be overwritten.
        return Task.CompletedTask;
    }
}
