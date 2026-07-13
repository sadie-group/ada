using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;
using Ada.Networking.Writers.Rooms.Furniture;

namespace Ada.Game.Rooms.Furniture.Interactors.Wired;

public class WiredEffectKickUserInteractor(IRoomWiredService wiredService) : AbstractRoomFurnitureItemInteractor
{
    public override List<string> InteractionTypes => [
        FurnitureItemInteractionType.WiredEffectKickUser
    ];

    public override async Task OnTriggerAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser)
    {
        var wiredData = item.WiredData;
        var input = wiredData?.Message ?? "";
        var furnitureItem = item.PlayerFurnitureItem.FurnitureItem;
        
        await roomUser.NetworkObject.WriteToStreamAsync(new WiredMessageEffectWriter
        {
            StuffTypeSelectionEnabled = false,
            MaxItemsSelected = 5,
            SelectedItemIds = [],
            WiredEffectType = furnitureItem.AssetId,
            Id = item.Id,
            Input = input,
            IntParams = [],
            StuffTypeSelectionCode = 0,
            Type = wiredService.GetWiredCode(furnitureItem.InteractionType ?? ""),
            DelayInPulses = 0,
            ConflictingTriggerIds = []
        });
    }
}