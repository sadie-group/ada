using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Game.Rooms.Services.Wired;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;
using Microsoft.Extensions.DependencyInjection;

namespace Ada.Game.Rooms.Wired.Effects;

public class WiredEffectCallAnotherStackStrategy(IServiceProvider serviceProvider) : IWiredEffectStrategy
{
    private const int MaxCallDepth = 5;

    private static readonly AsyncLocal<int> CallDepth = new();

    public string InteractionType => FurnitureItemInteractionType.WiredEffectCallAnotherStack;

    public async Task ExecuteAsync(
        IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto effect,
        IRoomUser? userWhoTriggered)
    {
        var selectedItems = effect.WiredData?.SelectedItems;

        if (selectedItems == null || selectedItems.Count == 0 || CallDepth.Value >= MaxCallDepth)
        {
            return;
        }
        
        var wiredService = serviceProvider.GetRequiredService<IRoomWiredService>();

        CallDepth.Value++;

        try
        {
            foreach (var selected in selectedItems)
            {
                var trigger = room.Room.FurnitureItems.FirstOrDefault(x =>
                    x.Id == selected.Id &&
                    (x.PlayerFurnitureItem.FurnitureItem.InteractionType ?? "").Contains("_trg_"));

                if (trigger != null)
                {
                    await wiredService.RunTriggerForRoomAsync(room, trigger, userWhoTriggered);
                }
            }
        }
        finally
        {
            CallDepth.Value--;
        }
    }
}
