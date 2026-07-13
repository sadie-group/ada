using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;
using Ada.Db.Models.Constants;
using Ada.Networking.Writers.Rooms.Furniture;

namespace Ada.Game.Rooms.Furniture.Interactors.Wired;

public class WiredTriggerUserWalksOnInteractor(IRoomWiredService wiredService,
    ServerRoomConstants roomConstants) : AbstractRoomFurnitureItemInteractor
{
    public override List<string> InteractionTypes =>
    [
        FurnitureItemInteractionType.WiredTriggerUserWalksOnFurniture
    ];

    public override async Task OnTriggerAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser)
    {
        var wiredData = item.WiredData;
        
        var selectedItemIds = wiredData?
            .SelectedItems
            .Select(x => x.Id)
            .ToList() ?? [];
        
        var furnitureItem = item.PlayerFurnitureItem.FurnitureItem;
        
        await roomUser.NetworkObject.WriteToStreamAsync(new WiredTriggerWriter
        {
            StuffTypeSelectionEnabled = false,
            MaxItemsSelected = roomConstants.WiredMaxFurnitureSelection,
            SelectedItemIds = selectedItemIds,
            AssetId = furnitureItem.AssetId,
            Id = item.Id,
            IntParameters = [],
            StuffTypeSelectionCode = 0,
            TriggerConfig = wiredService.GetWiredCode(furnitureItem.InteractionType ?? ""),
            ConflictingEffectIds = []
        });
    }
}