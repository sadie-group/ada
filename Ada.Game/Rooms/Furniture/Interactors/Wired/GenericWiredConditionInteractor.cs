using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Shared.Helpers;
using Ada.Db.Models.Constants;
using Ada.Networking.Writers.Rooms.Furniture;

namespace Ada.Game.Rooms.Furniture.Interactors.Wired;

public class GenericWiredConditionInteractor(IRoomWiredService wiredService,
    ServerRoomConstants roomConstants) : AbstractRoomFurnitureItemInteractor
{
    public override List<string> InteractionTypes =>
    [
        FurnitureItemInteractionType.WiredConditionFurnitureHasUsers,
        FurnitureItemInteractionType.WiredConditionNotFurnitureHasUsers,
        FurnitureItemInteractionType.WiredConditionTriggererOnFurniture,
        FurnitureItemInteractionType.WiredConditionNotTriggererOnFurniture,
        FurnitureItemInteractionType.WiredConditionUserCountInRoom,
        FurnitureItemInteractionType.WiredConditionNotUserCountInRoom,
        FurnitureItemInteractionType.WiredConditionTimeElapsedMore,
        FurnitureItemInteractionType.WiredConditionTimeElapsedLess,
        FurnitureItemInteractionType.WiredConditionFurnitureHasFurniture,
        FurnitureItemInteractionType.WiredConditionNotFurnitureHasFurniture,
        FurnitureItemInteractionType.WiredConditionTriggererWearsBadge,
        FurnitureItemInteractionType.WiredConditionNotTriggererWearsBadge,
        FurnitureItemInteractionType.WiredConditionTriggererWearsEffect,
        FurnitureItemInteractionType.WiredConditionNotTriggererWearsEffect,
        FurnitureItemInteractionType.WiredConditionTriggererHasHandItem,
        FurnitureItemInteractionType.WiredConditionDateRangeActive
    ];

    public override async Task OnTriggerAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser)
    {
        var wiredData = item.WiredData;

        var selectedItemIds = wiredData?
            .SelectedItems
            .Select(x => x.Id)
            .ToList() ?? [];

        await roomUser.NetworkObject.WriteToStreamAsync(new WiredConditionWriter
        {
            StuffTypeSelectionEnabled = false,
            MaxItemsSelected = roomConstants.WiredMaxFurnitureSelection,
            SelectedItemIds = selectedItemIds,
            AssetId = item.PlayerFurnitureItem.FurnitureItem.AssetId,
            Id = item.Id,
            Input = wiredData?.Message ?? "",
            IntParameters = WiredParameterHelpers.Deserialize(wiredData?.IntParameters),
            StuffTypeSelectionCode = 0,
            ConditionConfig = wiredService.GetWiredCode(item.PlayerFurnitureItem.FurnitureItem.InteractionType ?? "")
        });
    }
}
