using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms.Users;

namespace Ada.API.Interfaces.Game.Rooms.Services;

public interface IRoomWiredService
{
    IEnumerable<PlayerFurnitureItemPlacementDataDto> GetTriggers(
        string interactionType,
        IEnumerable<PlayerFurnitureItemPlacementDataDto> roomItems,
        string requiredMessage = "",
        List<int>? requiredSelectedIds = null);
    
    IEnumerable<PlayerFurnitureItemPlacementDataDto> GetEffectsForTrigger(
        PlayerFurnitureItemPlacementDataDto trigger,
        IEnumerable<PlayerFurnitureItemPlacementDataDto> roomItems);

    Task RunTriggerForRoomAsync(IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto trigger,
        IRoomUser userWhoTriggered);

    int GetWiredCode(string interactionType);

    Task SaveSettingsAsync(
        PlayerFurnitureItemPlacementDataDto placementData,
        PlayerFurnitureItemWiredDataDto wiredData);
}