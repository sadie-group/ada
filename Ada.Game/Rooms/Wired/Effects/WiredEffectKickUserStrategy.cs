using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Services.Wired;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;

namespace Ada.Game.Rooms.Wired.Effects;

public class WiredEffectKickUserStrategy : IWiredEffectStrategy
{
    public string InteractionType => FurnitureItemInteractionType.WiredEffectKickUser;

    public async Task ExecuteAsync(
        IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto effect,
        IRoomUser? userWhoTriggered)
    {
        if (userWhoTriggered == null || userWhoTriggered.Player.Player.Id == room.Room.OwnerId)
        {
            return;
        }

        await room.UserRepository.TryRemoveAsync(userWhoTriggered.Player.Player.Id, true, true);

        var message = effect.WiredData?.Message;

        if (!string.IsNullOrEmpty(message))
        {
            await userWhoTriggered.Player.SendAlertAsync(message);
        }
    }
}
