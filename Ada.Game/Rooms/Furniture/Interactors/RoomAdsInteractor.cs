using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;

namespace Ada.Game.Rooms.Furniture.Interactors;

public class RoomAdsInteractor : AbstractRoomFurnitureItemInteractor
{
    public override List<string> InteractionTypes => [FurnitureItemInteractionType.RoomAdsBg];

    public override Task OnPlaceAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser)
    {
        item.PlayerFurnitureItem.MetaData = "offsetZ=0;offsetY=0;offsetX=0;clickUrl=;imageUrl=;";
        return Task.CompletedTask;
    }
}