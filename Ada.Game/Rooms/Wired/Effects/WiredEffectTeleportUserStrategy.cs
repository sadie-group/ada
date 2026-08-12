using System.Drawing;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Services.Wired;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;

namespace Ada.Game.Rooms.Wired.Effects;

public class WiredEffectTeleportUserStrategy : IWiredEffectStrategy
{
    public string InteractionType => FurnitureItemInteractionType.WiredEffectTeleportToFurniture;

    public async Task ExecuteAsync(
        IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto effect,
        IRoomUser? userWhoTriggered)
    {
        var selectedItems = effect.WiredData?.SelectedItems;

        if (userWhoTriggered == null || selectedItems == null || selectedItems.Count == 0)
        {
            return;
        }

        var target = selectedItems.ElementAt(Random.Shared.Next(selectedItems.Count));

        var roomItem = room.Room.FurnitureItems.FirstOrDefault(x => x.Id == target.Id);

        if (roomItem == null)
        {
            return;
        }

        await userWhoTriggered.SetPositionAsync(new Point(roomItem.PositionX, roomItem.PositionY));
    }
}
