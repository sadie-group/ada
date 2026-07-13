using Ada.API.DTOs.Players.Furniture;
using Ada.Core.Enums.Miscellaneous;

namespace Ada.API.Interfaces.Game.Rooms.Furniture;

public interface IRoomFurnitureItemHelperService
{
    Task CycleInteractionStateForItemAsync(
        IRoomLogic room, 
        PlayerFurnitureItemPlacementDataDto roomFurnitureItem);

    Task UpdateMetaDataForItemAsync(
        IRoomLogic room, 
        PlayerFurnitureItemPlacementDataDto roomFurnitureItem, 
        string metaData);

    Task BroadcastItemUpdateToRoomAsync(
        IRoomLogic room, 
        PlayerFurnitureItemPlacementDataDto roomFurnitureItem);

    ObjectDataKey GetObjectDataKeyForItem(PlayerFurnitureItemPlacementDataDto furnitureItem);
    Dictionary<string, string> GetObjectDataForItem(PlayerFurnitureItemPlacementDataDto furnitureItem);
}