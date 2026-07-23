using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Game.Rooms.Services.Wired;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Shared.Helpers;

namespace Ada.Game.Rooms.Wired.Effects;

public class WiredEffectMuteTriggererStrategy(
    IRoomFloodProtectionService floodProtectionService) : IWiredEffectStrategy
{
    public string InteractionType => FurnitureItemInteractionType.WiredEffectMuteTriggerer;

    public async Task ExecuteAsync(
        IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto effect,
        IRoomUser? userWhoTriggered)
    {
        if (userWhoTriggered == null || userWhoTriggered.Player.Player.Id == room.Room.OwnerId)
        {
            return;
        }

        var parameters = WiredParameterHelpers.Deserialize(effect.WiredData?.IntParameters);
        var minutes = parameters.Count > 0 && parameters[0] > 0 ? parameters[0] : 1;

        floodProtectionService.MuteFor(userWhoTriggered.Player.Player.Id, minutes * 60);

        var message = effect.WiredData?.Message;

        if (!string.IsNullOrEmpty(message))
        {
            await userWhoTriggered.SendWhisperAsync(message);
        }
    }
}
