using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Services.Wired;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;

namespace Ada.Game.Rooms.Wired.Effects;

public class WiredEffectResetTimersStrategy(IWiredTimerService timerService) : IWiredEffectStrategy
{
    public string InteractionType => FurnitureItemInteractionType.WiredEffectResetTimers;

    public Task ExecuteAsync(
        IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto effect,
        IRoomUser? userWhoTriggered)
    {
        timerService.Reset(room.Room.Id);
        return Task.CompletedTask;
    }
}
