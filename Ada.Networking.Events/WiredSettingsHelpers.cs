using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.WordFilter;
using Ada.Core.Enums.Game.WordFilter;
using Ada.Core.Shared.Extensions;
using Ada.Core.Shared.Helpers;
using Ada.Db.Models.Constants;

namespace Ada.Networking.Events;

public static class WiredSettingsHelpers
{
    public const int MaxDelayInPulses = 120;

    public static PlayerFurnitureItemWiredDataDto Build(
        PlayerFurnitureItemPlacementDataDto roomItem,
        IEnumerable<PlayerFurnitureItemPlacementDataDto> selectedItems,
        string message,
        List<int> parameters,
        int delayInPulses,
        ServerRoomConstants roomConstants,
        IWordFilterService wordFilterService)
    {
        var boundedItems = selectedItems
            .Take(roomConstants.WiredMaxFurnitureSelection)
            .ToList();

        var filteredMessage = wordFilterService
            .Filter(message, WordFilterContext.Chat)
            .FilteredText
            .Truncate(roomConstants.MaxChatMessageLength);

        return new PlayerFurnitureItemWiredDataDto
        {
            PlayerFurnitureItemPlacementDataId = roomItem.Id,
            PlacementData = roomItem,
            SelectedItems = boundedItems,
            Message = filteredMessage,
            IntParameters = WiredParameterHelpers.Serialize(parameters),
            Delay = Math.Clamp(delayInPulses, 0, MaxDelayInPulses)
        };
    }
}
