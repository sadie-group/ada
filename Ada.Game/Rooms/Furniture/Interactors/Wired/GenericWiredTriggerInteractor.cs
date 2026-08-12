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

public class GenericWiredTriggerInteractor(IRoomWiredService wiredService,
    ServerRoomConstants roomConstants) : AbstractRoomFurnitureItemInteractor
{
    public override List<string> InteractionTypes =>
    [
        FurnitureItemInteractionType.WiredTriggerSaysSomething,
        FurnitureItemInteractionType.WiredTriggerEnterRoom,
        FurnitureItemInteractionType.WiredTriggerUserWalksOffFurniture,
        FurnitureItemInteractionType.WiredTriggerPeriodically,
        FurnitureItemInteractionType.WiredTriggerPeriodicallyLong,
        FurnitureItemInteractionType.WiredTriggerAtGivenTime,
        FurnitureItemInteractionType.WiredTriggerFurnitureStateChanged
    ];

    public override async Task OnTriggerAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser)
    {
        var wiredData = item.WiredData;
        
        var selectedItemIds = wiredData?
            .SelectedItems
            .Select(x => x.Id)
            .ToList() ?? [];

        var input = wiredData?.Message ?? "";
        
        await roomUser.NetworkObject.WriteToStreamAsync(new WiredTriggerWriter
        {
            StuffTypeSelectionEnabled = false,
            MaxItemsSelected = roomConstants.WiredMaxFurnitureSelection,
            SelectedItemIds = selectedItemIds,
            AssetId = 0,
            Id = 0,
            Input = input,
            IntParameters = WiredParameterHelpers.Deserialize(wiredData?.IntParameters),
            StuffTypeSelectionCode = 0,
            TriggerConfig = wiredService.GetWiredCode(item.PlayerFurnitureItem.FurnitureItem.InteractionType ?? ""),
            ConflictingEffectIds = []
        });
    }
}