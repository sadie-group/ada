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

public class GenericWiredEffectInteractor(IRoomWiredService wiredService,
    ServerRoomConstants roomConstants) : AbstractRoomFurnitureItemInteractor
{
    public override List<string> InteractionTypes =>
    [
        FurnitureItemInteractionType.WiredEffectToggleFurnitureState,
        FurnitureItemInteractionType.WiredEffectTeleportToFurniture,
        FurnitureItemInteractionType.WiredEffectResetTimers,
        FurnitureItemInteractionType.WiredEffectMuteTriggerer,
        FurnitureItemInteractionType.WiredEffectCallAnotherStack,
        FurnitureItemInteractionType.WiredEffectMoveRotateFurniture,
        FurnitureItemInteractionType.WiredEffectMoveFurnitureToClosestUser,
        FurnitureItemInteractionType.WiredEffectFleeFromClosestUser,
        FurnitureItemInteractionType.WiredEffectChangeFurnitureDirection
    ];

    public override async Task OnTriggerAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser)
    {
        var wiredData = item.WiredData;
        var furnitureItem = item.PlayerFurnitureItem.FurnitureItem;

        var selectedItemIds = wiredData?
            .SelectedItems
            .Select(x => x.Id)
            .ToList() ?? [];

        await roomUser.NetworkObject.WriteToStreamAsync(new WiredMessageEffectWriter
        {
            StuffTypeSelectionEnabled = false,
            MaxItemsSelected = roomConstants.WiredMaxFurnitureSelection,
            SelectedItemIds = selectedItemIds,
            WiredEffectType = furnitureItem.AssetId,
            Id = item.Id,
            Input = wiredData?.Message ?? "",
            IntParams = WiredParameterHelpers.Deserialize(wiredData?.IntParameters),
            StuffTypeSelectionCode = 0,
            Type = wiredService.GetWiredCode(furnitureItem.InteractionType ?? ""),
            DelayInPulses = wiredData?.Delay ?? 0,
            ConflictingTriggerIds = []
        });
    }
}
